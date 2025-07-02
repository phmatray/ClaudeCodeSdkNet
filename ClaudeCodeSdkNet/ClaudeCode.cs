using System.Runtime.CompilerServices;
using ClaudeCodeSdkNet.Internal;
using ClaudeCodeSdkNet.Types;
using Microsoft.Extensions.Logging;

namespace ClaudeCodeSdkNet;

public static class ClaudeCode
{
    /// <summary>
    /// Query Claude Code with a prompt and optional configuration.
    /// </summary>
    /// <param name="prompt">The prompt to send to Claude</param>
    /// <param name="options">Optional configuration options</param>
    /// <param name="claudePath">Optional path to Claude CLI executable</param>
    /// <param name="logger">Optional logger instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of messages from Claude</returns>
    public static async IAsyncEnumerable<Message> QueryAsync(
        string prompt,
        ClaudeCodeOptions? options = null,
        string? claudePath = null,
        ILogger? logger = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
        }

        var client = new InternalClient(claudePath, options, logger as ILogger<InternalClient>);
        
        await foreach (var message in client.QueryAsync(prompt, cancellationToken))
        {
            yield return message;
        }
    }

    /// <summary>
    /// Query Claude Code and collect all messages into a list.
    /// </summary>
    /// <param name="prompt">The prompt to send to Claude</param>
    /// <param name="options">Optional configuration options</param>
    /// <param name="claudePath">Optional path to Claude CLI executable</param>
    /// <param name="logger">Optional logger instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of all messages from Claude</returns>
    public static async Task<List<Message>> QueryAllAsync(
        string prompt,
        ClaudeCodeOptions? options = null,
        string? claudePath = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<Message>();
        
        await foreach (var message in QueryAsync(prompt, options, claudePath, logger, cancellationToken))
        {
            messages.Add(message);
        }
        
        return messages;
    }

    /// <summary>
    /// Query Claude Code and return only the final result message.
    /// </summary>
    /// <param name="prompt">The prompt to send to Claude</param>
    /// <param name="options">Optional configuration options</param>
    /// <param name="claudePath">Optional path to Claude CLI executable</param>
    /// <param name="logger">Optional logger instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The final result message, or null if no result was returned</returns>
    public static async Task<ResultMessage?> QueryResultAsync(
        string prompt,
        ClaudeCodeOptions? options = null,
        string? claudePath = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ResultMessage? result = null;
        
        await foreach (var message in QueryAsync(prompt, options, claudePath, logger, cancellationToken))
        {
            if (message is ResultMessage resultMessage)
            {
                result = resultMessage;
            }
        }
        
        return result;
    }
}