using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ClaudeCodeSdkNet.Types;
using Spectre.Console;
using Spectre.Console.Cli;
using UserStoryGenerator.Models;
using UserStoryGenerator.Services;

namespace UserStoryGenerator.Commands;

public class GenerateCommand : AsyncCommand<GenerateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Path to the repository to analyze")]
        [CommandArgument(0, "[PATH]")]
        public string? Path { get; init; }

        [Description("Type of project (e.g., DotNetLibrary, NodeJsExpress, Generic)")]
        [CommandOption("-t|--type")]
        [DefaultValue(ProjectType.Generic)]
        public ProjectType ProjectType { get; init; }

        [Description("Template to use (enhancement, security, performance, api, testing)")]
        [CommandOption("--template")]
        [DefaultValue("enhancement")]
        public string Template { get; init; } = "enhancement";

        [Description("Output file path (defaults to USER_STORIES.md in current directory)")]
        [CommandOption("-o|--output")]
        public string? Output { get; init; }

        [Description("Maximum number of user stories to generate")]
        [CommandOption("-n|--number")]
        [DefaultValue(20)]
        public int MaxStories { get; init; }

        [Description("Claude model to use")]
        [CommandOption("-m|--model")]
        public string? Model { get; init; }

        [Description("Custom prompt (overrides template)")]
        [CommandOption("-p|--prompt")]
        public string? CustomPrompt { get; init; }

        [Description("Show detailed metrics after generation")]
        [CommandOption("--metrics")]
        [DefaultValue(false)]
        public bool ShowMetrics { get; init; }

        [Description("Suppress all output except errors")]
        [CommandOption("-q|--quiet")]
        [DefaultValue(false)]
        public bool Quiet { get; init; }
    }

    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var path = settings.Path ?? Directory.GetCurrentDirectory();
        var outputPath = settings.Output ?? Path.Combine(Directory.GetCurrentDirectory(), "USER_STORIES.md");

        if (!Directory.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]Error: Directory '{path}' does not exist[/]");
            return 1;
        }

        var templates = StoryTemplate.GetDefaultTemplates();
        if (!templates.TryGetValue(settings.Template.ToLower(), out var template))
        {
            AnsiConsole.MarkupLine($"[red]Error: Unknown template '{settings.Template}'[/]");
            AnsiConsole.MarkupLine($"[yellow]Available templates: {string.Join(", ", templates.Keys)}[/]");
            return 1;
        }

        if (!settings.Quiet)
        {
            AnsiConsole.Write(
                new FigletText("Story Generator")
                    .Centered()
                    .Color(Color.Blue));

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[yellow]Configuration[/]")
                .AddColumn("Setting", c => c.Width(20))
                .AddColumn("Value");

            table.AddRow("Repository", path);
            table.AddRow("Project Type", settings.ProjectType.ToString());
            table.AddRow("Template", template.Name);
            table.AddRow("Max Stories", settings.MaxStories.ToString());
            table.AddRow("Output", outputPath);
            if (!string.IsNullOrEmpty(settings.Model))
                table.AddRow("Model", settings.Model);

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        var service = new UserStoryGeneratorService();

        try
        {
            string content = string.Empty;
            ResultMessage? result = null;
            TimeSpan duration = TimeSpan.Zero;

            if (settings.ShowMetrics || !settings.Quiet)
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Star)
                    .StartAsync($"[yellow]Analyzing {Path.GetFileName(path)} and generating user stories...[/]", 
                    async ctx =>
                    {
                        var metrics = await service.GenerateWithMetricsAsync(
                            path,
                            settings.ProjectType,
                            template,
                            settings.CustomPrompt,
                            settings.Model,
                            settings.MaxStories);

                        result = metrics.Result;
                        content = metrics.Content;
                        duration = metrics.Duration;
                    });
            }
            else
            {
                content = await service.GenerateUserStoriesAsync(
                    path,
                    settings.ProjectType,
                    template,
                    settings.CustomPrompt,
                    settings.Model,
                    settings.MaxStories);
            }

            // Save the output
            await File.WriteAllTextAsync(outputPath, content);

            if (!settings.Quiet)
            {
                AnsiConsole.WriteLine();
                
                var panel = new Panel($"[green]✓ User stories generated successfully![/]\n\nSaved to: [blue]{outputPath}[/]")
                    .Header("[bold]Success[/]")
                    .BorderColor(Color.Green)
                    .Expand();

                AnsiConsole.Write(panel);

                if (settings.ShowMetrics && result != null)
                {
                    AnsiConsole.WriteLine();
                    var metricsTable = new Table()
                        .Border(TableBorder.Rounded)
                        .Title("[yellow]Generation Metrics[/]")
                        .AddColumn("Metric")
                        .AddColumn("Value");

                    metricsTable.AddRow("Duration", $"{duration.TotalSeconds:F1} seconds");
                    
                    if (result.Usage != null)
                    {
                        metricsTable.AddRow("Input Tokens", result.Usage.InputTokens.ToString("N0"));
                        metricsTable.AddRow("Output Tokens", result.Usage.OutputTokens.ToString("N0"));
                        metricsTable.AddRow("Total Tokens", result.Usage.TotalTokens.ToString("N0"));
                    }
                    
                    if (result.Cost.HasValue)
                    {
                        metricsTable.AddRow("Cost", $"${result.Cost:F4}");
                    }

                    AnsiConsole.Write(metricsTable);
                }

                // Show preview
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Preview:[/]");
                
                var lines = content.Split('\n').Take(15);
                foreach (var line in lines)
                {
                    if (line.StartsWith("##"))
                        AnsiConsole.MarkupLine($"[bold yellow]{Markup.Escape(line)}[/]");
                    else if (line.StartsWith("**US-"))
                        AnsiConsole.MarkupLine($"[bold cyan]{Markup.Escape(line)}[/]");
                    else if (line.StartsWith("###"))
                        AnsiConsole.MarkupLine($"[bold blue]{Markup.Escape(line)}[/]");
                    else
                        AnsiConsole.MarkupLine($"[gray]{Markup.Escape(line)}[/]");
                }
                
                AnsiConsole.MarkupLine("\n[gray]... (see output file for complete list)[/]");
            }

            return 0;
        }
        catch (ClaudeCodeSdkNet.Exceptions.CLINotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            AnsiConsole.MarkupLine("[yellow]Please install Claude from https://claude.ai/download[/]");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            if (!settings.Quiet)
            {
                AnsiConsole.WriteException(ex);
            }
            return 1;
        }
    }
}