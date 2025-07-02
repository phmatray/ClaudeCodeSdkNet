# API Key Management with Nuke

The UserStoryGenerator tool requires an Anthropic API key to function. This document explains how to manage your API key using the Nuke build system.

## Why Use Nuke for API Key Management?

If you're having trouble setting the `ANTHROPIC_API_KEY` environment variable, Nuke provides a secure way to:
- Pass the API key directly to the tool without permanently modifying your environment
- Keep the API key out of your shell history (when using parameter files)
- Run the tool with different API keys for different projects

## Available Targets

### SetupApiKey

Sets up the Anthropic API key and provides instructions for permanent configuration.

```bash
# Set up the API key
./build.sh SetupApiKey --anthropic-api-key "your-actual-api-key-here"
```

This will:
1. Show you how to add the key to your shell configuration file (`.zshrc` or `.bashrc`)
2. Show you the export command for the current session
3. Create a temporary `.anthropic-api-key` file that you can source

**Note**: The `.anthropic-api-key` file is automatically ignored by git.

### RunWithApiKey

Runs the story-gen tool with the API key passed as an environment variable.

```bash
# Run with default settings (current directory, enhancement template)
./build.sh RunWithApiKey --anthropic-api-key "your-actual-api-key-here"

# Run with custom directory and template
./build.sh RunWithApiKey --anthropic-api-key "your-actual-api-key-here" --work-dir "/path/to/project" --template "security"
```

## Security Best Practices

### Using a Parameter File (Recommended)

Instead of passing the API key on the command line (which may be saved in shell history), use a parameter file:

1. Create a file named `nuke.parameters` in the project root:
   ```json
   {
     "anthropic-api-key": "your-actual-api-key-here"
   }
   ```

2. Add it to `.gitignore`:
   ```
   nuke.parameters
   ```

3. Run the commands without the API key parameter:
   ```bash
   ./build.sh SetupApiKey
   ./build.sh RunWithApiKey --work-dir "/path/to/project"
   ```

### Using Environment Variables

You can also set the API key as an environment variable that Nuke will use:

```bash
# Set for current session
export ANTHROPIC_API_KEY="your-actual-api-key-here"

# Or use the Nuke environment variable format
export NUKE_ANTHROPIC_API_KEY="your-actual-api-key-here"
```

## Example Workflow

1. **First Time Setup**:
   ```bash
   # Install the tool
   ./build.sh BuildAndInstall
   
   # Set up API key
   ./build.sh SetupApiKey --anthropic-api-key "sk-ant-..."
   
   # Source the temporary file
   source .anthropic-api-key
   ```

2. **Generate Stories**:
   ```bash
   # Using environment variable (after sourcing)
   story-gen generate /path/to/project
   
   # Or using Nuke with the API key
   ./build.sh RunWithApiKey --work-dir "/path/to/project" --template "enhancement"
   ```

3. **Batch Processing**:
   ```bash
   # For multiple projects, use RunWithApiKey in a loop
   for project in /path/to/project1 /path/to/project2; do
     ./build.sh RunWithApiKey --anthropic-api-key "sk-ant-..." --work-dir "$project"
   done
   ```

## Troubleshooting

If you're still getting API key errors:

1. **Check the key format**: Ensure your key starts with `sk-ant-`
2. **Check for typos**: API keys are case-sensitive
3. **Test with curl**:
   ```bash
   curl https://api.anthropic.com/v1/messages \
     -H "x-api-key: your-api-key-here" \
     -H "anthropic-version: 2023-06-01" \
     -H "content-type: application/json" \
     -d '{"model":"claude-3-sonnet-20240229","messages":[{"role":"user","content":"Hello"}],"max_tokens":10}'
   ```

4. **Check Claude Code installation**:
   ```bash
   which claude
   claude --version
   ```

5. **Verify the tool sees the key**:
   ```bash
   ANTHROPIC_API_KEY="your-key" story-gen generate . --quiet
   ```