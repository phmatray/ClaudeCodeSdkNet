using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using ClaudeCodeSdkNet.Exceptions;
using ClaudeCodeSdkNet.Types;
using Microsoft.Extensions.Logging;

namespace ClaudeCodeSdkNet.Transport;

public class SubprocessCLITransport : IAsyncDisposable
{
    private const int MaxStderrBytes = 100_000;
    private readonly string _claudePath;
    private readonly ClaudeCodeOptions? _options;
    private readonly ILogger<SubprocessCLITransport>? _logger;
    private Process? _process;
    private readonly StringBuilder _stderrBuffer = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public SubprocessCLITransport(string? claudePath = null, ClaudeCodeOptions? options = null, ILogger<SubprocessCLITransport>? logger = null)
    {
        _claudePath = claudePath ?? FindClaudeCLI();
        _options = options;
        _logger = logger;
        _logger?.LogInformation("Using Claude CLI at: {Path}", _claudePath);
    }

    private static string FindClaudeCLI()
    {
        var possiblePaths = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            possiblePaths.Add(Path.Combine(appData, "Claude", "resources", "claude.exe"));
            possiblePaths.Add(Path.Combine(appData, "Claude", "claude.exe"));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            // Check nvm installations dynamically
            var nvmPath = Path.Combine(home, ".nvm", "versions", "node");
            if (Directory.Exists(nvmPath))
            {
                var nodeDirs = Directory.GetDirectories(nvmPath);
                foreach (var nodeDir in nodeDirs.OrderByDescending(d => d)) // Latest version first
                {
                    var claudePath = Path.Combine(nodeDir, "bin", "claude");
                    possiblePaths.Add(claudePath);
                }
            }
            
            possiblePaths.Add("/usr/local/bin/claude");
            possiblePaths.Add(Path.Combine(home, ".local", "bin", "claude"));
            // Claude Desktop app last (may not support 'code' subcommand)
            possiblePaths.Add("/Applications/Claude.app/Contents/MacOS/claude");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            possiblePaths.Add("/usr/local/bin/claude");
            possiblePaths.Add("/usr/bin/claude");
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            possiblePaths.Add(Path.Combine(home, ".local", "bin", "claude"));
        }

        // Check PATH environment variable
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            var pathDirs = pathEnv.Split(Path.PathSeparator);
            foreach (var dir in pathDirs)
            {
                var claudeInPath = Path.Combine(dir, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "claude.exe" : "claude");
                possiblePaths.Add(claudeInPath);
            }
        }

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                // Skip Claude Desktop app if we find Claude Code CLI
                if (path.Contains("/Applications/Claude.app") && possiblePaths.Any(p => p != path && File.Exists(p)))
                {
                    continue;
                }
                return path;
            }
        }

        throw new CLINotFoundException(
            "Claude Code CLI not found. Please ensure Claude Code (not Claude Desktop) is installed. " +
            "Install with: npm install -g @anthropic-ai/claude-cli\n" +
            "Or visit https://docs.anthropic.com/en/docs/claude-code for installation instructions."
        );
    }

    public async IAsyncEnumerable<string> StreamJsonLinesAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var args = BuildCommandArgs(prompt);
        
        _logger?.LogDebug("Starting Claude CLI with args: {Args}", string.Join(" ", args));

        var processStartInfo = new ProcessStartInfo
        {
            FileName = _claudePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };

        foreach (var arg in args)
        {
            processStartInfo.ArgumentList.Add(arg);
        }

        // Set environment variable to identify SDK
        processStartInfo.Environment["CLAUDE_SDK"] = "claude-code-sdk-net";

        _process = new Process { StartInfo = processStartInfo };

        try
        {
            if (!_process.Start())
            {
                throw new CLIConnectionException("Failed to start Claude CLI process");
            }

            // Start reading stderr in background
            _ = Task.Run(async () => await ReadStderrAsync(_process.StandardError), _cancellationTokenSource.Token);

            // Read stdout line by line
            string? line;
            while ((line = await _process.StandardOutput.ReadLineAsync(cancellationToken)) != null)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    yield return line;
                }
            }

            await _process.WaitForExitAsync(cancellationToken);

            if (_process.ExitCode != 0)
            {
                var stderr = _stderrBuffer.ToString();
                _logger?.LogError("Claude CLI stderr: {Stderr}", stderr);
                
                // Provide more helpful error messages for common issues
                var errorMessage = $"Claude CLI exited with code {_process.ExitCode}";
                if (stderr.Contains("ANTHROPIC_API_KEY") || stderr.Contains("API key"))
                {
                    errorMessage = "API key not found. Please set ANTHROPIC_API_KEY environment variable";
                }
                else if (stderr.Contains("401") || stderr.Contains("Unauthorized"))
                {
                    errorMessage = "Invalid API key. Please check your ANTHROPIC_API_KEY";
                }
                else if (!string.IsNullOrWhiteSpace(stderr))
                {
                    errorMessage = $"{errorMessage}: {stderr}";
                }
                
                throw new ProcessException(errorMessage, _process.ExitCode, stderr);
            }
            else if (_stderrBuffer.Length > 0)
            {
                _logger?.LogWarning("Claude CLI stderr (exit code 0): {Stderr}", _stderrBuffer.ToString());
            }
        }
        finally
        {
            await CleanupAsync();
        }
    }

    private List<string> BuildCommandArgs(string prompt)
    {
        var args = new List<string> { "code", "--output-format", "json" };

        if (_options != null)
        {
            if (_options.AllowedTools?.Count > 0)
            {
                args.Add("--allowed-tools");
                args.Add(string.Join(",", _options.AllowedTools));
            }

            if (_options.DisallowedTools?.Count > 0)
            {
                args.Add("--disallowed-tools");
                args.Add(string.Join(",", _options.DisallowedTools));
            }

            if (_options.SystemPrompts != null)
            {
                foreach (var systemPrompt in _options.SystemPrompts)
                {
                    args.Add("--system-prompt");
                    args.Add(systemPrompt);
                }
            }

            if (_options.McpServers != null)
            {
                foreach (var server in _options.McpServers)
                {
                    args.Add("--mcp-server");
                    args.Add(JsonSerializer.Serialize(server));
                }
            }

            if (_options.PermissionMode.HasValue)
            {
                args.Add("--permission-mode");
                args.Add(_options.PermissionMode.Value.ToString().ToLowerInvariant());
            }

            if (_options.Continue == true)
            {
                args.Add("--continue");
            }

            if (_options.Resume == true)
            {
                args.Add("--resume");
            }

            if (_options.MaxTurns.HasValue)
            {
                args.Add("--max-turns");
                args.Add(_options.MaxTurns.Value.ToString());
            }

            if (!string.IsNullOrEmpty(_options.Model))
            {
                args.Add("--model");
                args.Add(_options.Model);
            }

            if (!string.IsNullOrEmpty(_options.Cwd))
            {
                args.Add("--cwd");
                args.Add(_options.Cwd);
            }

            if (!string.IsNullOrEmpty(_options.SaveConversationPath))
            {
                args.Add("--save-conversation");
                args.Add(_options.SaveConversationPath);
            }

            if (_options.MaxInputChars.HasValue)
            {
                args.Add("--max-input-chars");
                args.Add(_options.MaxInputChars.Value.ToString());
            }

            if (!string.IsNullOrEmpty(_options.MemoryDir))
            {
                args.Add("--memory");
                args.Add(_options.MemoryDir);
            }

            if (!string.IsNullOrEmpty(_options.ApiKey))
            {
                args.Add("--api-key");
                args.Add(_options.ApiKey);
            }

            if (!string.IsNullOrEmpty(_options.ApiKeyEnvVar))
            {
                args.Add("--api-key-env-var");
                args.Add(_options.ApiKeyEnvVar);
            }

            if (!string.IsNullOrEmpty(_options.AnthropicApiBase))
            {
                args.Add("--anthropic-api-base");
                args.Add(_options.AnthropicApiBase);
            }

            if (_options.PromptCaching == true)
            {
                args.Add("--prompt-caching");
            }

            if (!string.IsNullOrEmpty(_options.ProjectFile))
            {
                args.Add("--project-file");
                args.Add(_options.ProjectFile);
            }

            if (_options.AutoProjectFile == true)
            {
                args.Add("--auto-project-file");
            }

            if (_options.EditAutocomplete == true)
            {
                args.Add("--edit-autocomplete");
            }

            if (_options.Interactive == true)
            {
                args.Add("--interactive");
            }

            if (!string.IsNullOrEmpty(_options.Theme))
            {
                args.Add("--theme");
                args.Add(_options.Theme);
            }
        }

        args.Add(prompt);
        return args;
    }

    private async Task ReadStderrAsync(StreamReader stderr)
    {
        try
        {
            char[] buffer = new char[1024];
            int bytesRead;
            int totalBytes = 0;

            while ((bytesRead = await stderr.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                if (totalBytes + bytesRead > MaxStderrBytes)
                {
                    _stderrBuffer.Append("[stderr truncated]");
                    break;
                }

                _stderrBuffer.Append(buffer, 0, bytesRead);
                totalBytes += bytesRead;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error reading stderr");
        }
    }

    private async Task CleanupAsync()
    {
        if (_process != null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(true);
                    await _process.WaitForExitAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during process cleanup");
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }

        _cancellationTokenSource.Cancel();
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupAsync();
        _cancellationTokenSource.Dispose();
    }
}