using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using ClaudeCodeSdkNet.Types;
using Spectre.Console;
using Spectre.Console.Cli;
using UserStoryGenerator.Models;
using UserStoryGenerator.Services;

namespace UserStoryGenerator.Commands;

public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }
}

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

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
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

            // Use streaming approach with visual feedback
            var contentBuilder = new StringBuilder();
            var messageCount = 0;
            var lastUpdateTime = DateTime.UtcNow;
            
            await AnsiConsole.Live(new Panel("[yellow]Initializing...[/]")
                .Header("[bold]Story Generation Progress[/]")
                .BorderColor(Color.Blue)
                .Expand())
                .StartAsync(async ctx =>
                {
                    await foreach (var progress in service.GenerateUserStoriesStreamingAsync(
                        path,
                        settings.ProjectType,
                        template,
                        settings.CustomPrompt,
                        settings.Model,
                        settings.MaxStories))
                    {
                        messageCount = progress.MessageCount;
                        
                        switch (progress.Type)
                        {
                            case ProgressType.Connecting:
                                ctx.UpdateTarget(new Panel($"[yellow]{progress.Message}[/]\n\n[dim]Establishing connection to Claude CLI...[/]")
                                    .Header("[bold]Story Generation Progress[/]")
                                    .BorderColor(Color.Blue)
                                    .Expand());
                                break;
                                
                            case ProgressType.System:
                                ctx.UpdateTarget(new Panel($"[green]Connected![/]\n\n[cyan]{progress.Message}[/]\n[dim]Messages received: {messageCount}[/]")
                                    .Header("[bold]Story Generation Progress[/]")
                                    .BorderColor(Color.Green)
                                    .Expand());
                                break;
                                
                            case ProgressType.Content:
                                if (!string.IsNullOrEmpty(progress.Content))
                                {
                                    contentBuilder.AppendLine(progress.Content);
                                    
                                    // Update display only every 500ms to avoid flicker
                                    if ((DateTime.UtcNow - lastUpdateTime).TotalMilliseconds > 500)
                                    {
                                        var preview = GetContentPreview(contentBuilder.ToString());
                                        ctx.UpdateTarget(new Panel($"[green]Generating user stories...[/]\n\n{preview}\n\n[dim]Messages: {messageCount} | Characters: {contentBuilder.Length:N0}[/]")
                                            .Header("[bold]Story Generation Progress[/]")
                                            .BorderColor(Color.Green)
                                            .Expand());
                                        lastUpdateTime = DateTime.UtcNow;
                                    }
                                }
                                break;
                                
                            case ProgressType.Result:
                                result = progress.Result;
                                duration = progress.Duration ?? TimeSpan.Zero;
                                ctx.UpdateTarget(new Panel($"[green]Generation complete![/]\n\n[dim]Total messages: {messageCount} | Duration: {duration.TotalSeconds:F1}s[/]")
                                    .Header("[bold]Story Generation Progress[/]")
                                    .BorderColor(Color.Green)
                                    .Expand());
                                break;
                                
                            case ProgressType.Complete:
                                content = progress.Content ?? contentBuilder.ToString();
                                break;
                                
                            case ProgressType.Error:
                                var errorMessage = progress.Message ?? "Unknown error";
                                var errorPanel = new Panel($"[red]Error occurred![/]\n\n{errorMessage}");
                                
                                // Add specific help for common errors
                                if (errorMessage.Contains("401") || errorMessage.Contains("Unauthorized"))
                                {
                                    errorPanel = new Panel($"[red]Authentication Error![/]\n\n{errorMessage}\n\n[yellow]Please check your API key:[/]\n[blue]export ANTHROPIC_API_KEY=your-api-key-here[/]");
                                }
                                else if (errorMessage.Contains("code: 1") || errorMessage.Contains("exit code 1"))
                                {
                                    errorPanel = new Panel($"[red]Claude CLI Error![/]\n\n{errorMessage}\n\n[yellow]This often means:[/]\n- API key is not set\n- Claude Desktop is being used instead of Claude Code CLI\n- Network connectivity issues");
                                }
                                
                                errorPanel.Header("[bold]Story Generation Progress[/]")
                                    .BorderColor(Color.Red)
                                    .Expand();
                                    
                                ctx.UpdateTarget(errorPanel);
                                break;
                        }
                    }
                });

            // Check if we got any content
            if (string.IsNullOrWhiteSpace(content))
            {
                AnsiConsole.MarkupLine("[yellow]Warning: No user stories were generated. This might be due to:[/]");
                AnsiConsole.MarkupLine("[yellow]- Claude CLI not responding properly[/]");
                AnsiConsole.MarkupLine("[yellow]- API key issues[/]");
                AnsiConsole.MarkupLine("[yellow]- Network connectivity problems[/]");
                AnsiConsole.MarkupLine("[yellow]Try running with --metrics to see more details[/]");
                return 1;
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
    
    private static string GetContentPreview(string content)
    {
        var lines = content.Split('\n')
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
        
        if (lines.Count == 0)
            return "[dim]Waiting for content...[/]";
        
        var preview = new StringBuilder();
        var storyCount = lines.Count(l => l.StartsWith("**US-"));
        
        if (storyCount > 0)
        {
            preview.AppendLine($"[bold cyan]Stories generated: {storyCount}[/]");
            preview.AppendLine();
        }
        
        // Show last few meaningful lines
        var lastLines = lines.TakeLast(5).ToList();
        foreach (var line in lastLines)
        {
            if (line.StartsWith("##"))
                preview.AppendLine($"[yellow]{Markup.Escape(line)}[/]");
            else if (line.StartsWith("**US-"))
                preview.AppendLine($"[cyan]{Markup.Escape(line)}[/]");
            else if (line.StartsWith("- "))
                preview.AppendLine($"[dim]{Markup.Escape(line)}[/]");
            else
                preview.AppendLine($"[gray]{Markup.Escape(line.Truncate(80))}[/]");
        }
        
        return preview.ToString();
    }
}