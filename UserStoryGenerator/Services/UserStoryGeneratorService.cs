using System.Text;
using ClaudeCodeSdkNet;
using ClaudeCodeSdkNet.Types;
using UserStoryGenerator.Models;

namespace UserStoryGenerator.Services;

public class UserStoryGeneratorService
{
    private readonly ClaudeCodeOptions _defaultOptions;

    public UserStoryGeneratorService()
    {
        _defaultOptions = new ClaudeCodeOptions
        {
            Model = "claude-3-5-sonnet-20241022",
            MaxTurns = 1
        };
    }

    public async Task<string> GenerateUserStoriesAsync(
        string projectPath,
        ProjectType projectType,
        StoryTemplate template,
        string? customPrompt = null,
        string? model = null,
        int? maxStories = null,
        CancellationToken cancellationToken = default)
    {
        var options = _defaultOptions with
        {
            Cwd = projectPath,
            Model = model ?? _defaultOptions.Model
        };

        var prompt = BuildPrompt(projectPath, projectType, template, customPrompt, maxStories);
        var storyBuilder = new StringBuilder();

        await foreach (var message in ClaudeCode.QueryAsync(prompt, options, cancellationToken: cancellationToken))
        {
            if (message is AssistantMessage assistantMsg)
            {
                foreach (var block in assistantMsg.Content)
                {
                    if (block is TextBlock textBlock)
                    {
                        storyBuilder.AppendLine(textBlock.Text);
                    }
                }
            }
        }

        return storyBuilder.ToString();
    }

    private string BuildPrompt(
        string projectPath,
        ProjectType projectType,
        StoryTemplate template,
        string? customPrompt,
        int? maxStories)
    {
        if (!string.IsNullOrWhiteSpace(customPrompt))
        {
            return customPrompt;
        }

        var projectName = Path.GetFileName(projectPath);
        var storyCount = maxStories ?? 20;

        var prompt = $@"Analyze the {projectType} project '{projectName}' in the current directory and generate comprehensive user stories for {template.Description}.

Focus on these areas:
{string.Join("\n", template.FocusAreas.Select((area, index) => $"{index + 1}. {area}"))}

For each user story, use this exact format:

**US-XXX: As a [role], I want [feature] so that [benefit]**
- Given [context]
- When [action]
- Then [expected result]
- And [additional criteria if needed]

Important guidelines:
- Group the stories by epic/category
- Make each story specific to the actual code and architecture you find
- Focus on actionable improvements based on the codebase analysis
- Generate exactly {storyCount} meaningful user stories
- Consider the project type ({projectType}) when suggesting improvements

Start with '## User Stories for {projectName} - {template.Name}' as the title.";

        return prompt;
    }

    public async Task<(ResultMessage? Result, string Content, TimeSpan Duration)> GenerateWithMetricsAsync(
        string projectPath,
        ProjectType projectType,
        StoryTemplate template,
        string? customPrompt = null,
        string? model = null,
        int? maxStories = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var content = await GenerateUserStoriesAsync(
            projectPath, 
            projectType, 
            template, 
            customPrompt, 
            model, 
            maxStories, 
            cancellationToken);
        
        var duration = DateTime.UtcNow - startTime;

        // Try to get the result message for metrics
        var options = _defaultOptions with
        {
            Cwd = projectPath,
            Model = model ?? _defaultOptions.Model
        };
        
        var prompt = BuildPrompt(projectPath, projectType, template, customPrompt, maxStories);
        var result = await ClaudeCode.QueryResultAsync(prompt, options, cancellationToken: cancellationToken);

        return (result, content, duration);
    }
}