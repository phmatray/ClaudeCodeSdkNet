using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeCodeSdkNet.Types;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(UserMessage), "user")]
[JsonDerivedType(typeof(AssistantMessage), "assistant")]
[JsonDerivedType(typeof(SystemMessage), "system")]
[JsonDerivedType(typeof(ResultMessage), "result")]
public abstract record Message
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

public record UserMessage : Message
{
    [JsonPropertyName("type")]
    public override string Type => "user";
    
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

public record AssistantMessage : Message
{
    [JsonPropertyName("type")]
    public override string Type => "assistant";
    
    [JsonPropertyName("content")]
    public required List<ContentBlock> Content { get; init; }
}

public record SystemMessage : Message
{
    [JsonPropertyName("type")]
    public override string Type => "system";
    
    [JsonPropertyName("subtype")]
    public required string Subtype { get; init; }
    
    [JsonPropertyName("data")]
    public JsonElement? Data { get; init; }
}

public record ResultMessage : Message
{
    [JsonPropertyName("type")]
    public override string Type => "result";
    
    [JsonPropertyName("result")]
    public required string Result { get; init; }
    
    [JsonPropertyName("usage")]
    public Usage? Usage { get; init; }
    
    [JsonPropertyName("timeTaken")]
    public double? TimeTaken { get; init; }
    
    [JsonPropertyName("cacheCreationTokens")]
    public int? CacheCreationTokens { get; init; }
    
    [JsonPropertyName("cacheReadTokens")]
    public int? CacheReadTokens { get; init; }
    
    [JsonPropertyName("cost")]
    public double? Cost { get; init; }
}

public record Usage
{
    [JsonPropertyName("inputTokens")]
    public int InputTokens { get; init; }
    
    [JsonPropertyName("outputTokens")]
    public int OutputTokens { get; init; }
    
    [JsonPropertyName("totalTokens")]
    public int TotalTokens { get; init; }
}