# User Story Generator

An AI-powered CLI tool that analyzes software repositories and generates comprehensive user stories using Claude AI. Perfect for managing technical debt, planning enhancements, and documenting improvement opportunities across multiple projects.

## Features

- 🚀 **Multi-Repository Support**: Process single repositories or batch process hundreds
- 📋 **Multiple Templates**: Enhancement, Security, Performance, API, Testing templates
- 🎯 **Project Type Awareness**: Tailored analysis for different project types (Web APIs, Libraries, SPAs, etc.)
- 📊 **Rich CLI Interface**: Beautiful console output with progress tracking
- 🔧 **Highly Configurable**: Custom prompts, output formats, and generation parameters
- 📈 **Metrics & Reporting**: Token usage, cost tracking, and batch summaries

## Installation

### As a .NET Tool

```bash
dotnet tool install -g UserStoryGenerator
```

### From Source

```bash
git clone https://github.com/yourusername/UserStoryGenerator
cd UserStoryGenerator
dotnet build
```

## Usage

### Generate Stories for a Single Repository

```bash
# Current directory with default settings
story-gen generate

# Specific repository with options
story-gen generate /path/to/repo -t DotNetWebApi --template security -n 30

# With metrics and custom output
story-gen generate ./my-project --metrics -o TECH_DEBT_STORIES.md
```

### List Available Options

```bash
# Show all project types
story-gen list --types

# Show all templates
story-gen list --templates
```

### Batch Process Multiple Repositories

```bash
# Create a file with repository paths (one per line)
echo "/path/to/repo1" > repos.txt
echo "/path/to/repo2" >> repos.txt

# Process all repositories
story-gen batch repos.txt -o ./user-stories -c 5
```

## Project Types

- `Generic` - Any type of project (default)
- `DotNetLibrary` - .NET class libraries
- `DotNetWebApi` - ASP.NET Core Web APIs
- `DotNetMvc` - ASP.NET Core MVC apps
- `DotNetBlazor` - Blazor applications
- `JavaSpringBoot` - Spring Boot apps
- `NodeJsExpress` - Express.js apps
- `ReactApp` - React SPAs
- `PythonDjango` - Django apps
- And many more...

## Templates

### Enhancement (default)
General improvements covering code quality, features, testing, and documentation.

### Security
Security-focused stories for authentication, encryption, validation, and vulnerability remediation.

### Performance
Performance optimization stories for caching, async processing, and resource usage.

### API Design
API-specific stories for RESTful design, versioning, and documentation.

### Testing
Comprehensive testing stories for unit, integration, and E2E testing.

## Command Reference

### Generate Command

```bash
story-gen generate [PATH] [OPTIONS]
```

Options:
- `-t, --type` - Project type (default: Generic)
- `--template` - Template to use (default: enhancement)
- `-o, --output` - Output file path
- `-n, --number` - Max stories to generate (default: 20)
- `-m, --model` - Claude model to use
- `-p, --prompt` - Custom prompt (overrides template)
- `--metrics` - Show generation metrics
- `-q, --quiet` - Suppress output

### Batch Command

```bash
story-gen batch <INPUT_FILE> [OPTIONS]
```

Options:
- `-o, --output-dir` - Output directory (default: ./user-stories)
- `-t, --type` - Default project type
- `--template` - Template to use
- `-n, --number` - Stories per repository
- `-c, --concurrent` - Concurrent generations (default: 3)
- `--continue-on-error` - Don't stop on errors
- `--summary` - Generate summary report

## Examples

### Analyze a .NET Library

```bash
story-gen generate ~/projects/MyLibrary -t DotNetLibrary --template enhancement
```

### Security Audit Multiple APIs

```bash
story-gen batch api-repos.txt --template security -o ./security-audits
```

### Custom Analysis with Metrics

```bash
story-gen generate . -p "Analyze for GDPR compliance issues" --metrics
```

## Output Format

Generated user stories follow this format:

```markdown
**US-001: As a developer, I want [feature] so that [benefit]**
- Given [context]
- When [action]
- Then [expected result]
- And [additional criteria]
```

## Requirements

- .NET 9.0 Runtime
- Claude CLI installed (https://claude.ai/download)
- Valid Claude API access

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT