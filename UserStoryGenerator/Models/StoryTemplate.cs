namespace UserStoryGenerator.Models;

public class StoryTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProjectType ProjectType { get; set; }
    public List<string> FocusAreas { get; set; } = new();
    public string PromptTemplate { get; set; } = string.Empty;
    
    public static Dictionary<string, StoryTemplate> GetDefaultTemplates()
    {
        return new Dictionary<string, StoryTemplate>
        {
            ["enhancement"] = new StoryTemplate
            {
                Name = "Enhancement",
                Description = "General enhancements for code quality, features, and performance",
                ProjectType = ProjectType.Generic,
                FocusAreas = new List<string>
                {
                    "Code quality improvements (refactoring, better patterns, performance)",
                    "New API features that would benefit developers",
                    "Bug fixes or potential issues you identify",
                    "Developer experience improvements",
                    "Testing and reliability enhancements",
                    "Documentation needs"
                }
            },
            ["security"] = new StoryTemplate
            {
                Name = "Security",
                Description = "Security-focused user stories",
                ProjectType = ProjectType.Generic,
                FocusAreas = new List<string>
                {
                    "Authentication and authorization improvements",
                    "Data encryption and protection",
                    "Input validation and sanitization",
                    "Security headers and configurations",
                    "Vulnerability remediation",
                    "Security testing and monitoring"
                }
            },
            ["performance"] = new StoryTemplate
            {
                Name = "Performance",
                Description = "Performance optimization user stories",
                ProjectType = ProjectType.Generic,
                FocusAreas = new List<string>
                {
                    "Query optimization",
                    "Caching strategies",
                    "Memory usage optimization",
                    "Async/parallel processing",
                    "Resource pooling",
                    "Load testing and benchmarking"
                }
            },
            ["api"] = new StoryTemplate
            {
                Name = "API Design",
                Description = "API design and improvement stories",
                ProjectType = ProjectType.DotNetWebApi,
                FocusAreas = new List<string>
                {
                    "RESTful API design improvements",
                    "API versioning strategy",
                    "Request/response validation",
                    "Error handling and status codes",
                    "API documentation (OpenAPI/Swagger)",
                    "Rate limiting and throttling"
                }
            },
            ["testing"] = new StoryTemplate
            {
                Name = "Testing",
                Description = "Testing and quality assurance stories",
                ProjectType = ProjectType.Generic,
                FocusAreas = new List<string>
                {
                    "Unit test coverage improvements",
                    "Integration testing scenarios",
                    "E2E testing implementation",
                    "Test data management",
                    "Performance testing",
                    "Security testing"
                }
            }
        };
    }
}