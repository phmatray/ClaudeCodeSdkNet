# API Key Troubleshooting Guide

This guide helps resolve common API key issues with the UserStoryGenerator tool.

## Common Issues and Solutions

### 1. "ANTHROPIC_API_KEY environment variable is not set"

This error occurs when the tool cannot find your API key. Here are several solutions:

#### Solution A: Set Environment Variable
```bash
# For current session only
export ANTHROPIC_API_KEY="your-api-key-here"

# For permanent setup (add to ~/.zshrc or ~/.bashrc)
echo 'export ANTHROPIC_API_KEY="your-api-key-here"' >> ~/.zshrc
source ~/.zshrc
```

#### Solution B: Use Nuke Build System
```bash
# Install and run with API key in one command
./build.sh RunWithApiKey --anthropic-api-key "your-api-key-here"

# Or set it up for manual sourcing
./build.sh SetupApiKey --anthropic-api-key "your-api-key-here"
source .anthropic-api-key
```

### 2. "Claude Desktop opens instead of using Claude Code CLI"

This happens when Claude Desktop is installed but Claude Code CLI is not properly installed.

#### Solution:
```bash
# Install Claude Code CLI
npm install -g @anthropic-ai/claude-cli

# Verify installation
which claude
claude --version

# The tool should now use the CLI instead of opening the desktop app
```

### 3. "Invalid API key" or 401 Unauthorized

Your API key may be incorrect or expired.

#### Solution:
1. Get a valid API key from https://console.anthropic.com/account/keys
2. Ensure the key starts with `sk-ant-`
3. Test the key:
   ```bash
   curl https://api.anthropic.com/v1/messages \
     -H "x-api-key: your-api-key-here" \
     -H "anthropic-version: 2023-06-01" \
     -H "content-type: application/json" \
     -d '{"model":"claude-3-sonnet-20240229","messages":[{"role":"user","content":"Hello"}],"max_tokens":10}'
   ```

### 4. "Process exited with code 1"

This generic error can have multiple causes:

#### Diagnostics:
```bash
# Check if API key is visible to the tool
env | grep ANTHROPIC_API_KEY

# Test with explicit environment variable
ANTHROPIC_API_KEY="your-key" story-gen generate . --quiet

# Use Nuke to bypass environment issues
./build.sh RunWithApiKey --anthropic-api-key "your-key" --work-dir "."
```

## Best Practices

### Secure API Key Storage

1. **Never commit API keys to git**
   - The `.anthropic-api-key` file is already in `.gitignore`
   - Use environment variables or Nuke parameters

2. **Use Nuke parameter files** (most secure)
   ```json
   // nuke.parameters (add to .gitignore)
   {
     "anthropic-api-key": "your-api-key-here"
   }
   ```

3. **Use system keychain** (macOS)
   ```bash
   # Store in keychain
   security add-generic-password -a "$USER" -s "ANTHROPIC_API_KEY" -w "your-key"
   
   # Retrieve in shell profile
   export ANTHROPIC_API_KEY=$(security find-generic-password -a "$USER" -s "ANTHROPIC_API_KEY" -w)
   ```

## Quick Test Commands

```bash
# Test 1: Direct environment variable
ANTHROPIC_API_KEY="your-key" story-gen generate . --quiet

# Test 2: Using Nuke
./build.sh RunWithApiKey --anthropic-api-key "your-key"

# Test 3: After setting up
source .anthropic-api-key
story-gen generate .
```

## Still Having Issues?

1. Check Claude Code is installed (not just Claude Desktop)
2. Ensure your API key is valid and has sufficient credits
3. Check network connectivity to api.anthropic.com
4. Review the tool's output for specific error messages
5. Use the `--metrics` flag for more detailed information

For persistent issues, please file a bug report with:
- The exact error message
- Output of `which claude` and `claude --version`
- Your operating system and .NET version
- Steps to reproduce the issue