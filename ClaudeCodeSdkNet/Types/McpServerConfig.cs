using System.Text.Json.Serialization;

namespace ClaudeCodeSdkNet.Types;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(McpStdioServerConfig), "stdio")]
[JsonDerivedType(typeof(McpSSEServerConfig), "sse")]
[JsonDerivedType(typeof(McpHttpServerConfig), "http")]
public abstract record McpServerConfig
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

public record McpStdioServerConfig : McpServerConfig
{
    [JsonPropertyName("type")]
    public override string Type => "stdio";
    
    [JsonPropertyName("command")]
    public required string Command { get; init; }
    
    [JsonPropertyName("args")]
    public List<string>? Args { get; init; }
    
    [JsonPropertyName("env")]
    public Dictionary<string, string>? Env { get; init; }
}

public record McpSSEServerConfig : McpServerConfig
{
    [JsonPropertyName("type")]
    public override string Type => "sse";
    
    [JsonPropertyName("url")]
    public required string Url { get; init; }
}

public record McpHttpServerConfig : McpServerConfig
{
    [JsonPropertyName("type")]
    public override string Type => "http";
    
    [JsonPropertyName("url")]
    public required string Url { get; init; }
}