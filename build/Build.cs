using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Serilog;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    
    [Parameter("Anthropic API Key for testing")]
    [Secret]
    readonly string AnthropicApiKey;

    [Solution] readonly Solution Solution;
    
    AbsolutePath SourceDirectory => RootDirectory / "UserStoryGenerator";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath PackagesDirectory => OutputDirectory / "packages";
    
    Project UserStoryGeneratorProject => Solution.GetProject("UserStoryGenerator");

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DotNetClean(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration));
                
            OutputDirectory.DeleteDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Produces(PackagesDirectory / "*.nupkg")
        .Executes(() =>
        {
            // First build in Release mode
            DotNetBuild(s => s
                .SetProjectFile(UserStoryGeneratorProject)
                .SetConfiguration(Configuration.Release)
                .EnableNoRestore());
                
            DotNetPack(s => s
                .SetProject(UserStoryGeneratorProject)
                .SetConfiguration(Configuration.Release)
                .SetOutputDirectory(PackagesDirectory)
                .EnableNoBuild()
                .EnableNoRestore());
        });

    Target Install => _ => _
        .DependsOn(Pack)
        .Description("Installs the UserStoryGenerator as a global dotnet tool")
        .Executes(() =>
        {
            // First, try to uninstall any existing version
            try 
            {
                DotNetToolUninstall(s => s
                    .SetPackageName("UserStoryGenerator")
                    .EnableGlobal());
            }
            catch 
            {
                // Tool might not be installed, that's okay
            }

            var packageFile = PackagesDirectory.GlobFiles("*.nupkg").FirstOrDefault();
            if (packageFile == null)
            {
                throw new Exception("No package file found in output directory");
            }

            DotNetToolInstall(s => s
                .SetPackageName("UserStoryGenerator")
                .EnableGlobal()
                .AddSources(PackagesDirectory));
                
            Log.Information("UserStoryGenerator has been installed as a global tool!");
            Log.Information("You can now use 'story-gen' command from anywhere in your terminal.");
        });

    Target BuildAndInstall => _ => _
        .DependsOn(Clean, Install)
        .Description("Cleans, builds in Release mode, and installs the tool globally")
        .Executes(() =>
        {
            Log.Information("Build and installation completed successfully!");
        });

    Target Test => _ => _
        .DependsOn(Install)
        .Description("Tests the installed tool")
        .Executes(() =>
        {
            // Test that the tool is available
            var process = ProcessTasks.StartProcess("story-gen", "--version", logOutput: true);
            process.WaitForExit();
            
            if (process.ExitCode == 0)
            {
                Log.Information("story-gen tool is working correctly!");
            }
            else
            {
                Log.Error("story-gen tool test failed!");
            }
        });
        
    Target SetupApiKey => _ => _
        .Description("Sets up the Anthropic API key in your environment")
        .Requires(() => AnthropicApiKey)
        .Executes(() =>
        {
            var shell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash";
            var shellConfig = shell.Contains("zsh") ? "~/.zshrc" : "~/.bashrc";
            
            Log.Information($"To set the API key permanently, add this to your {shellConfig}:");
            Log.Information($"export ANTHROPIC_API_KEY=\"{AnthropicApiKey}\"");
            Log.Information("");
            Log.Information("For the current session, run:");
            Log.Information($"export ANTHROPIC_API_KEY=\"{AnthropicApiKey}\"");
            
            // Save to a temporary file for easy sourcing
            var tempFile = RootDirectory / ".anthropic-api-key";
            tempFile.WriteAllText($"export ANTHROPIC_API_KEY=\"{AnthropicApiKey}\"\n");
            
            Log.Information("");
            Log.Information($"Or source the temporary file: source {tempFile}");
        });
        
    [Parameter("Working directory for story generation")]
    readonly string WorkDir;
    
    [Parameter("Template to use for story generation")]
    readonly string Template;
    
    Target RunWithApiKey => _ => _
        .DependsOn(Install)
        .Description("Runs story-gen with the provided API key")
        .Requires(() => AnthropicApiKey)
        .Executes(() =>
        {
            var workDir = WorkDir ?? RootDirectory.ToString();
            var template = Template ?? "enhancement";
            
            var process = ProcessTasks.StartProcess(
                "story-gen", 
                $"generate \"{workDir}\" --template {template}",
                workingDirectory: RootDirectory,
                environmentVariables: new Dictionary<string, string>
                {
                    ["ANTHROPIC_API_KEY"] = AnthropicApiKey
                },
                logOutput: true);
                
            process.WaitForExit();
            
            if (process.ExitCode != 0)
            {
                throw new Exception($"story-gen failed with exit code {process.ExitCode}");
            }
        });
}