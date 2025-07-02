using Spectre.Console;
using Spectre.Console.Cli;
using UserStoryGenerator.Commands;

// Check for API key early
var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    AnsiConsole.MarkupLine("[red]Error: ANTHROPIC_API_KEY environment variable is not set[/]");
    AnsiConsole.MarkupLine("[yellow]Please set your API key:[/]");
    AnsiConsole.MarkupLine("  [blue]export ANTHROPIC_API_KEY=your-api-key-here[/]");
    AnsiConsole.MarkupLine("[dim]Get your API key from: https://console.anthropic.com/account/keys[/]");
    return 1;
}

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("story-gen");
    config.SetApplicationVersion("1.0.0");
    
    config.AddCommand<GenerateCommand>("generate")
        .WithAlias("gen")
        .WithDescription("Generate user stories for a repository")
        .WithExample("generate", "/path/to/repo", "-t", "DotNetLibrary", "--template", "enhancement")
        .WithExample("generate", ".", "-n", "30", "--metrics")
        .WithExample("generate", "/my/project", "--template", "security", "-o", "SECURITY_STORIES.md");
    
    config.AddCommand<ListCommand>("list")
        .WithAlias("ls")
        .WithDescription("List available project types and templates")
        .WithExample("list", "--types")
        .WithExample("list", "--templates");
    
    config.AddCommand<BatchCommand>("batch")
        .WithDescription("Generate user stories for multiple repositories")
        .WithExample("batch", "repos.txt", "-o", "./stories", "-c", "5")
        .WithExample("batch", "projects.txt", "--template", "security", "--continue-on-error");

    config.ValidateExamples();
});

return await app.RunAsync(args);