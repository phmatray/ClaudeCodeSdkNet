using ClaudeCodeSdkNet;
using ClaudeCodeSdkNet.Types;

namespace Examples;

public class QuickStart
{
    public static async Task BasicExample()
    {
        // Simple query - stream messages as they arrive
        await foreach (var message in ClaudeCode.QueryAsync("Write a hello world program in C#"))
        {
            switch (message)
            {
                case UserMessage userMsg:
                    Console.WriteLine($"User: {userMsg.Content}");
                    break;
                    
                case AssistantMessage assistantMsg:
                    Console.Write("Assistant: ");
                    foreach (var block in assistantMsg.Content)
                    {
                        if (block is TextBlock textBlock)
                        {
                            Console.Write(textBlock.Text);
                        }
                    }
                    Console.WriteLine();
                    break;
                    
                case ResultMessage resultMsg:
                    Console.WriteLine($"\nResult: {resultMsg.Result}");
                    if (resultMsg.Usage != null)
                    {
                        Console.WriteLine($"Tokens used: {resultMsg.Usage.TotalTokens}");
                    }
                    if (resultMsg.Cost.HasValue)
                    {
                        Console.WriteLine($"Cost: ${resultMsg.Cost:F4}");
                    }
                    break;
            }
        }
    }

    public static async Task ConfiguredExample()
    {
        // Query with configuration options
        var options = new ClaudeCodeOptions
        {
            Model = "claude-3-5-sonnet-20241022",
            MaxTurns = 3,
            Cwd = "/path/to/project",
            AllowedTools = new List<string> { "read_file", "write_file" },
            SystemPrompts = new List<string> { "You are a helpful coding assistant." }
        };

        var messages = await ClaudeCode.QueryAllAsync(
            "Help me refactor this code to be more efficient",
            options
        );

        foreach (var message in messages)
        {
            Console.WriteLine($"Message type: {message.Type}");
        }
    }

    public static async Task ResultOnlyExample()
    {
        // Get only the final result
        var result = await ClaudeCode.QueryResultAsync("What is 2 + 2?");
        
        if (result != null)
        {
            Console.WriteLine($"Answer: {result.Result}");
        }
    }

    public static async Task ErrorHandlingExample()
    {
        try
        {
            await foreach (var message in ClaudeCode.QueryAsync("Help me with my code"))
            {
                // Process messages
            }
        }
        catch (ClaudeCodeSdkNet.Exceptions.CLINotFoundException)
        {
            Console.WriteLine("Claude CLI is not installed. Please install it first.");
        }
        catch (ClaudeCodeSdkNet.Exceptions.ProcessException ex)
        {
            Console.WriteLine($"Claude CLI error (exit code {ex.ExitCode}): {ex.Message}");
            if (!string.IsNullOrEmpty(ex.StdError))
            {
                Console.WriteLine($"Error details: {ex.StdError}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }

    public static async Task CancellationExample()
    {
        using var cts = new CancellationTokenSource();
        
        // Cancel after 30 seconds
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            await foreach (var message in ClaudeCode.QueryAsync(
                "Write a comprehensive guide about C# async programming",
                cancellationToken: cts.Token))
            {
                // Process messages
                if (message is AssistantMessage)
                {
                    Console.Write(".");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nOperation was cancelled.");
        }
    }

    public static async Task MCPServerExample()
    {
        // Configure with MCP servers
        var options = new ClaudeCodeOptions
        {
            McpServers = new List<McpServerConfig>
            {
                new McpStdioServerConfig
                {
                    Name = "filesystem",
                    Command = "npx",
                    Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", "/tmp" }
                },
                new McpSSEServerConfig
                {
                    Name = "example-sse",
                    Url = "http://localhost:8080/sse"
                }
            }
        };

        await foreach (var message in ClaudeCode.QueryAsync(
            "List the files in /tmp using the filesystem MCP server",
            options))
        {
            // Process messages
        }
    }
}