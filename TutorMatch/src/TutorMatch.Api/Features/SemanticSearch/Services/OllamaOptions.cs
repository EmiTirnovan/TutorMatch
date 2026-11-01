namespace TutorMatch.Api.Features.SemanticSearch.Services;

/// <summary>
/// Optiuni de configurare pentru clientul Ollama.
/// </summary>
public class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "bge-m3";
    public int TimeoutSeconds { get; set; } = 5;
}

