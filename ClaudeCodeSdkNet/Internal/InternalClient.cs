using System.Runtime.CompilerServices;
using System.Text.Json;
using ClaudeCodeSdkNet.Transport;
using ClaudeCodeSdkNet.Types;
using Microsoft.Extensions.Logging;

namespace ClaudeCodeSdkNet.Internal;

internal class InternalClient
{
    private readonly string? _claudePath;
    private readonly ClaudeCodeOptions? _options;
    private readonly ILogger<InternalClient>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public InternalClient(string? claudePath = null, ClaudeCodeOptions? options = null, ILogger<InternalClient>? logger = null)
    {
        _claudePath = claudePath;
        _options = options;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async IAsyncEnumerable<Message> QueryAsync(
        string prompt, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var transport = new SubprocessCLITransport(_claudePath, _options, _logger as ILogger<SubprocessCLITransport>);
        
        await foreach (var line in transport.StreamJsonLinesAsync(prompt, cancellationToken))
        {
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Skip non-JSON lines (like status messages from CLI)
            if (!line.TrimStart().StartsWith("{") && !line.TrimStart().StartsWith("["))
            {
                _logger?.LogDebug("Skipping non-JSON line: {Line}", line);
                continue;
            }

            Message? message;
            
            try
            {
                message = ParseMessage(line);
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "Failed to parse JSON line: {Line}", line);
                // Continue processing instead of throwing
                continue;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error parsing message: {Line}", line);
                // Continue processing instead of throwing
                continue;
            }

            if (message != null)
            {
                yield return message;
            }
        }
    }

    private Message? ParseMessage(string jsonLine)
    {
        if (string.IsNullOrWhiteSpace(jsonLine))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(jsonLine);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeElement))
        {
            _logger?.LogWarning("Message missing 'type' field: {Json}", jsonLine);
            return null;
        }

        var type = typeElement.GetString();
        
        return type switch
        {
            "user" => ParseUserMessage(root),
            "assistant" => ParseAssistantMessage(root),
            "system" => ParseSystemMessage(root),
            "result" => ParseResultMessage(root),
            _ => LogAndReturnNull(type)
        };
    }

    private UserMessage ParseUserMessage(JsonElement element)
    {
        var content = element.GetProperty("content").GetString() ?? string.Empty;
        return new UserMessage { Content = content };
    }

    private AssistantMessage ParseAssistantMessage(JsonElement element)
    {
        var contentBlocks = new List<ContentBlock>();
        
        if (element.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var blockElement in contentElement.EnumerateArray())
            {
                var block = ParseContentBlock(blockElement);
                if (block != null)
                {
                    contentBlocks.Add(block);
                }
            }
        }

        return new AssistantMessage { Content = contentBlocks };
    }

    private ContentBlock? ParseContentBlock(JsonElement element)
    {
        if (!element.TryGetProperty("type", out var typeElement))
        {
            return null;
        }

        var type = typeElement.GetString();
        
        return type switch
        {
            "text" => new TextBlock 
            { 
                Text = element.GetProperty("text").GetString() ?? string.Empty 
            },
            "tool_use" => new ToolUseBlock
            {
                Id = element.GetProperty("id").GetString() ?? string.Empty,
                Name = element.GetProperty("name").GetString() ?? string.Empty,
                Input = element.TryGetProperty("input", out var inputElement) ? inputElement : null
            },
            "tool_result" => new ToolResultBlock
            {
                ToolUseId = element.GetProperty("tool_use_id").GetString() ?? string.Empty,
                Content = element.GetProperty("content").GetString() ?? string.Empty,
                IsError = element.TryGetProperty("is_error", out var errorElement) && errorElement.GetBoolean()
            },
            _ => null
        };
    }

    private SystemMessage ParseSystemMessage(JsonElement element)
    {
        var subtype = element.GetProperty("subtype").GetString() ?? string.Empty;
        JsonElement? data = element.TryGetProperty("data", out var dataElement) ? dataElement : null;
        
        return new SystemMessage
        {
            Subtype = subtype,
            Data = data
        };
    }

    private ResultMessage ParseResultMessage(JsonElement element)
    {
        var result = element.GetProperty("result").GetString() ?? string.Empty;
        
        Usage? usage = null;
        if (element.TryGetProperty("usage", out var usageElement))
        {
            usage = new Usage
            {
                InputTokens = usageElement.TryGetProperty("inputTokens", out var input) ? input.GetInt32() : 0,
                OutputTokens = usageElement.TryGetProperty("outputTokens", out var output) ? output.GetInt32() : 0,
                TotalTokens = usageElement.TryGetProperty("totalTokens", out var total) ? total.GetInt32() : 0
            };
        }

        return new ResultMessage
        {
            Result = result,
            Usage = usage,
            TimeTaken = element.TryGetProperty("timeTaken", out var time) ? time.GetDouble() : null,
            CacheCreationTokens = element.TryGetProperty("cacheCreationTokens", out var cacheCreate) ? cacheCreate.GetInt32() : null,
            CacheReadTokens = element.TryGetProperty("cacheReadTokens", out var cacheRead) ? cacheRead.GetInt32() : null,
            Cost = element.TryGetProperty("cost", out var cost) ? cost.GetDouble() : null
        };
    }

    private Message? LogAndReturnNull(string? type)
    {
        _logger?.LogWarning("Unknown message type: {Type}", type);
        return null;
    }
}