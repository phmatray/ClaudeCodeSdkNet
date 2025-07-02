using Spectre.Console.Cli;
using UserStoryGenerator.Commands;

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