namespace TutorMatch.Api.Features.SemanticSearch.Services;

/// <summary>
/// Optiuni de configurare pentru clientul Ollama.
/// </summary>
public class OllamaOptions
{
    public const string SectionName = "Ollama";
    public const int DefaultQueryTimeoutMilliseconds = 3500;

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "bge-m3";

    // HTTP timeout for any embedding call; kept generous so tutor indexing survives a model cold start.
    public int TimeoutSeconds { get; set; } = 5;

    // Strict budget for the user-facing search query embedding; past it the search falls back to keywords.
    public int QueryTimeoutMilliseconds { get; set; } = DefaultQueryTimeoutMilliseconds;
}
