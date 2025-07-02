using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;
using UserStoryGenerator.Models;

namespace UserStoryGenerator.Commands;

public class ListCommand : Command<ListCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("List project types")]
        [CommandOption("-t|--types")]
        [DefaultValue(false)]
        public bool ListTypes { get; init; }

        [Description("List available templates")]
        [CommandOption("--templates")]
        [DefaultValue(false)]
        public bool ListTemplates { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (!settings.ListTypes && !settings.ListTemplates)
        {
            // Default to showing both
            ShowProjectTypes();
            AnsiConsole.WriteLine();
            ShowTemplates();
        }
        else
        {
            if (settings.ListTypes)
            {
                ShowProjectTypes();
            }

            if (settings.ListTemplates)
            {
                if (settings.ListTypes) AnsiConsole.WriteLine();
                ShowTemplates();
            }
        }

        return 0;
    }

    private void ShowProjectTypes()
    {
        AnsiConsole.Write(new Rule("[yellow]Available Project Types[/]").RuleStyle("grey").LeftJustified());
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Type", c => c.Width(25))
            .AddColumn("Description");

        table.AddRow("Generic", "Any type of project (default)");
        table.AddRow("DotNetLibrary", ".NET class library or NuGet package");
        table.AddRow("DotNetWebApi", "ASP.NET Core Web API");
        table.AddRow("DotNetMvc", "ASP.NET Core MVC application");
        table.AddRow("DotNetBlazor", "Blazor WebAssembly or Server application");
        table.AddRow("JavaSpringBoot", "Spring Boot application");
        table.AddRow("JavaLibrary", "Java library or Maven package");
        table.AddRow("NodeJsExpress", "Express.js web application");
        table.AddRow("NodeJsLibrary", "Node.js library or npm package");
        table.AddRow("ReactApp", "React single-page application");
        table.AddRow("AngularApp", "Angular application");
        table.AddRow("VueApp", "Vue.js application");
        table.AddRow("PythonDjango", "Django web application");
        table.AddRow("PythonFlask", "Flask web application");
        table.AddRow("PythonLibrary", "Python library or pip package");
        table.AddRow("GoService", "Go microservice or application");
        table.AddRow("RustLibrary", "Rust library or crate");
        table.AddRow("MobileApp", "Mobile application (iOS/Android)");
        table.AddRow("Infrastructure", "Infrastructure as Code (Terraform, CloudFormation)");
        table.AddRow("Documentation", "Documentation repository");

        AnsiConsole.Write(table);
    }

    private void ShowTemplates()
    {
        AnsiConsole.Write(new Rule("[yellow]Available Templates[/]").RuleStyle("grey").LeftJustified());
        AnsiConsole.WriteLine();

        var templates = StoryTemplate.GetDefaultTemplates();
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Template", c => c.Width(15))
            .AddColumn("Description", c => c.Width(40))
            .AddColumn("Focus Areas");

        foreach (var (key, template) in templates)
        {
            var focusAreas = string.Join("\n", template.FocusAreas.Take(3).Select(a => $"• {a}"));
            if (template.FocusAreas.Count > 3)
            {
                focusAreas += $"\n• ... and {template.FocusAreas.Count - 3} more";
            }
            
            table.AddRow(
                $"[cyan]{key}[/]",
                template.Description,
                focusAreas
            );
        }

        AnsiConsole.Write(table);
    }
}