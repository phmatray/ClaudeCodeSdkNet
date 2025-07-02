using ClaudeCodeSdkNet.Types;

namespace UserStoryGenerator.Models;

public enum ProgressType
{
    Connecting,
    System,
    Content,
    Result,
    Complete,
    Error
}

public class GenerationProgress
{
    public ProgressType Type { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Content { get; init; }
    public ResultMessage? Result { get; init; }
    public int MessageCount { get; init; }
    public TimeSpan? Duration { get; init; }
    public Exception? Error { get; init; }
}