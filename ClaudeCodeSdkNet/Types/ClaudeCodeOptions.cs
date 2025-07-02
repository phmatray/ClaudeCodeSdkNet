using System.Text.Json.Serialization;

namespace ClaudeCodeSdkNet.Types;

public record ClaudeCodeOptions
{
    [JsonPropertyName("allowed_tools")]
    public List<string>? AllowedTools { get; init; }
    
    [JsonPropertyName("disallowed_tools")]
    public List<string>? DisallowedTools { get; init; }
    
    [JsonPropertyName("system_prompts")]
    public List<string>? SystemPrompts { get; init; }
    
    [JsonPropertyName("mcp_servers")]
    public List<McpServerConfig>? McpServers { get; init; }
    
    [JsonPropertyName("permission_mode")]
    public PermissionMode? PermissionMode { get; init; }
    
    [JsonPropertyName("continue")]
    public bool? Continue { get; init; }
    
    [JsonPropertyName("resume")]
    public bool? Resume { get; init; }
    
    [JsonPropertyName("max_turns")]
    public int? MaxTurns { get; init; }
    
    [JsonPropertyName("model")]
    public string? Model { get; init; }
    
    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }
    
    [JsonPropertyName("save_conversation_path")]
    public string? SaveConversationPath { get; init; }
    
    [JsonPropertyName("max_input_chars")]
    public int? MaxInputChars { get; init; }
    
    [JsonPropertyName("memory_dir")]
    public string? MemoryDir { get; init; }
    
    [JsonPropertyName("api_key")]
    public string? ApiKey { get; init; }
    
    [JsonPropertyName("api_key_env_var")]
    public string? ApiKeyEnvVar { get; init; }
    
    [JsonPropertyName("anthropic_api_base")]
    public string? AnthropicApiBase { get; init; }
    
    [JsonPropertyName("prompt_caching")]
    public bool? PromptCaching { get; init; }
    
    [JsonPropertyName("project_file")]
    public string? ProjectFile { get; init; }
    
    [JsonPropertyName("auto_project_file")]
    public bool? AutoProjectFile { get; init; }
    
    [JsonPropertyName("edit_autocomplete")]
    public bool? EditAutocomplete { get; init; }
    
    [JsonPropertyName("interactive")]
    public bool? Interactive { get; init; }
    
    [JsonPropertyName("theme")]
    public string? Theme { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<PermissionMode>))]
public enum PermissionMode
{
    [JsonPropertyName("default")]
    Default,
    
    [JsonPropertyName("acceptEdits")]
    AcceptEdits,
    
    [JsonPropertyName("bypassPermissions")]
    BypassPermissions
}