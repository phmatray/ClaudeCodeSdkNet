using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaudeCodeSdkNet.Types;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextBlock), "text")]
[JsonDerivedType(typeof(ToolUseBlock), "tool_use")]
[JsonDerivedType(typeof(ToolResultBlock), "tool_result")]
public abstract record ContentBlock
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

public record TextBlock : ContentBlock
{
    [JsonPropertyName("type")]
    public override string Type => "text";
    
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}

public record ToolUseBlock : ContentBlock
{
    [JsonPropertyName("type")]
    public override string Type => "tool_use";
    
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("input")]
    public JsonElement? Input { get; init; }
}

public record ToolResultBlock : ContentBlock
{
    [JsonPropertyName("type")]
    public override string Type => "tool_result";
    
    [JsonPropertyName("tool_use_id")]
    public required string ToolUseId { get; init; }
    
    [JsonPropertyName("content")]
    public required string Content { get; init; }
    
    [JsonPropertyName("is_error")]
    public bool IsError { get; init; }
}