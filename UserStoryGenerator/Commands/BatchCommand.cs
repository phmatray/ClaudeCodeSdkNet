using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;
using UserStoryGenerator.Models;
using UserStoryGenerator.Services;

namespace UserStoryGenerator.Commands;

public class BatchCommand : AsyncCommand<BatchCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("File containing list of repository paths (one per line)")]
        [CommandArgument(0, "<INPUT_FILE>")]
        public string InputFile { get; init; } = string.Empty;

        [Description("Output directory for generated stories")]
        [CommandOption("-o|--output-dir")]
        [DefaultValue("./user-stories")]
        public string OutputDirectory { get; init; } = "./user-stories";

        [Description("Default project type for all repositories")]
        [CommandOption("-t|--type")]
        [DefaultValue(ProjectType.Generic)]
        public ProjectType DefaultProjectType { get; init; }

        [Description("Template to use")]
        [CommandOption("--template")]
        [DefaultValue("enhancement")]
        public string Template { get; init; } = "enhancement";

        [Description("Maximum number of user stories per repository")]
        [CommandOption("-n|--number")]
        [DefaultValue(20)]
        public int MaxStories { get; init; }

        [Description("Number of concurrent generations")]
        [CommandOption("-c|--concurrent")]
        [DefaultValue(3)]
        public int ConcurrentGenerations { get; init; }

        [Description("Continue on error")]
        [CommandOption("--continue-on-error")]
        [DefaultValue(true)]
        public bool ContinueOnError { get; init; }

        [Description("Generate summary report")]
        [CommandOption("--summary")]
        [DefaultValue(true)]
        public bool GenerateSummary { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!File.Exists(settings.InputFile))
        {
            AnsiConsole.MarkupLine($"[red]Error: Input file '{settings.InputFile}' does not exist[/]");
            return 1;
        }

        var templates = StoryTemplate.GetDefaultTemplates();
        if (!templates.TryGetValue(settings.Template.ToLower(), out var template))
        {
            AnsiConsole.MarkupLine($"[red]Error: Unknown template '{settings.Template}'[/]");
            return 1;
        }

        // Read repository paths
        var repoPaths = await File.ReadAllLinesAsync(settings.InputFile);
        repoPaths = repoPaths.Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#")).ToArray();

        if (repoPaths.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No repository paths found in input file[/]");
            return 0;
        }

        // Create output directory
        Directory.CreateDirectory(settings.OutputDirectory);

        AnsiConsole.Write(
            new FigletText("Batch Generator")
                .Centered()
                .Color(Color.Blue));

        AnsiConsole.MarkupLine($"[bold]Processing {repoPaths.Length} repositories[/]");
        AnsiConsole.WriteLine();

        var service = new UserStoryGeneratorService();
        var results = new List<(string Path, bool Success, string? Error, TimeSpan Duration)>();

        // Create semaphore for concurrent processing
        using var semaphore = new SemaphoreSlim(settings.ConcurrentGenerations);
        var tasks = new List<Task>();

        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn(),
            })
            .StartAsync(async ctx =>
            {
                var progressTask = ctx.AddTask($"[yellow]Processing repositories[/]", maxValue: repoPaths.Length);

                foreach (var repoPath in repoPaths)
                {
                    await semaphore.WaitAsync();
                    
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            var startTime = DateTime.UtcNow;
                            var repoName = Path.GetFileName(repoPath);
                            progressTask.Description = $"[yellow]Processing {repoName}[/]";

                            if (!Directory.Exists(repoPath))
                            {
                                results.Add((repoPath, false, "Directory not found", TimeSpan.Zero));
                                return;
                            }

                            var outputFile = Path.Combine(settings.OutputDirectory, $"{repoName}_USER_STORIES.md");
                            
                            var content = await service.GenerateUserStoriesAsync(
                                repoPath,
                                settings.DefaultProjectType,
                                template,
                                maxStories: settings.MaxStories);

                            await File.WriteAllTextAsync(outputFile, content);
                            
                            var duration = DateTime.UtcNow - startTime;
                            results.Add((repoPath, true, null, duration));
                        }
                        catch (Exception ex)
                        {
                            results.Add((repoPath, false, ex.Message, TimeSpan.Zero));
                            if (!settings.ContinueOnError)
                            {
                                throw;
                            }
                        }
                        finally
                        {
                            progressTask.Increment(1);
                            semaphore.Release();
                        }
                    });

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            });

        // Display results
        AnsiConsole.WriteLine();
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[yellow]Processing Results[/]")
            .AddColumn("Repository", c => c.Width(40))
            .AddColumn("Status", c => c.Width(10))
            .AddColumn("Duration", c => c.Width(15))
            .AddColumn("Notes");

        foreach (var (path, success, error, duration) in results.OrderBy(r => r.Path))
        {
            var repoName = Path.GetFileName(path);
            var status = success ? "[green]Success[/]" : "[red]Failed[/]";
            var durationStr = success ? $"{duration.TotalSeconds:F1}s" : "-";
            var notes = error ?? "Generated successfully";

            table.AddRow(repoName, status, durationStr, notes);
        }

        AnsiConsole.Write(table);

        // Generate summary report
        if (settings.GenerateSummary)
        {
            var summaryPath = Path.Combine(settings.OutputDirectory, "BATCH_SUMMARY.md");
            var summary = GenerateSummaryReport(results, settings, template);
            await File.WriteAllTextAsync(summaryPath, summary);
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[green]Summary report saved to: {summaryPath}[/]");
        }

        var successCount = results.Count(r => r.Success);
        var failCount = results.Count(r => !r.Success);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Completed: {successCount} successful, {failCount} failed[/]");

        return failCount > 0 && !settings.ContinueOnError ? 1 : 0;
    }

    private string GenerateSummaryReport(
        List<(string Path, bool Success, string? Error, TimeSpan Duration)> results,
        Settings settings,
        StoryTemplate template)
    {
        var totalDuration = results.Where(r => r.Success).Sum(r => r.Duration.TotalSeconds);
        var avgDuration = results.Count(r => r.Success) > 0 
            ? totalDuration / results.Count(r => r.Success) 
            : 0;

        return $@"# Batch User Story Generation Summary

Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

## Configuration
- Template: {template.Name}
- Project Type: {settings.DefaultProjectType}
- Stories per Repository: {settings.MaxStories}
- Concurrent Generations: {settings.ConcurrentGenerations}

## Results
- Total Repositories: {results.Count}
- Successful: {results.Count(r => r.Success)}
- Failed: {results.Count(r => !r.Success)}
- Total Duration: {totalDuration:F1} seconds
- Average Duration: {avgDuration:F1} seconds per repository

## Repository Details

### Successful
{string.Join("\n", results.Where(r => r.Success).Select(r => $"- {Path.GetFileName(r.Path)} ({r.Duration.TotalSeconds:F1}s)"))}

### Failed
{string.Join("\n", results.Where(r => !r.Success).Select(r => $"- {Path.GetFileName(r.Path)}: {r.Error}"))}

## Output Files
All generated user stories are saved in: {Path.GetFullPath(settings.OutputDirectory)}
";
    }
}