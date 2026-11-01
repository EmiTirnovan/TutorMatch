using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TutorMatch.Api.Features.SemanticSearch.Services;

/// <summary>
/// Client HTTP pentru comunicarea cu Ollama prin endpoint-ul /api/embed.
/// </summary>
public class OllamaClient : IOllamaClient
{
    public const string HttpClientName = "Ollama";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaClient>? _logger;

    public OllamaClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IOptions<OllamaOptions>? options = null,
        ILogger<OllamaClient>? logger = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // Incarcam configuratia din IOptions sau direct din IConfiguration cu valori default
        var boundOptions = options?.Value ?? new OllamaOptions();
        var section = configuration.GetSection(OllamaOptions.SectionName);
        if (section.Exists())
        {
            boundOptions.BaseUrl = section["BaseUrl"] ?? boundOptions.BaseUrl;
            boundOptions.Model = section["Model"] ?? boundOptions.Model;
            if (int.TryParse(section["TimeoutSeconds"], out var timeout) && timeout > 0)
            {
                boundOptions.TimeoutSeconds = timeout;
            }
        }

        _options = boundOptions;
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger?.LogDebug("Generarea embedding-ului a fost omisa pentru text vid.");
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var baseUrl = !string.IsNullOrWhiteSpace(_options.BaseUrl)
                ? _options.BaseUrl.TrimEnd('/')
                : "http://localhost:11434";

            var endpointUri = client.BaseAddress != null
                ? new Uri(client.BaseAddress, "api/embed")
                : new Uri($"{baseUrl}/api/embed");

            var requestBody = new OllamaEmbedRequest
            {
                Model = !string.IsNullOrWhiteSpace(_options.Model) ? _options.Model : "bge-m3",
                Input = text
            };

            int timeoutSec = _options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 5;
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var response = await client.PostAsJsonAsync(endpointUri, requestBody, linkedCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning(
                    "Ollama /api/embed a returnat codul HTTP {StatusCode}.",
                    response.StatusCode);
                return null;
            }

            var embedResponse = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(cancellationToken: linkedCts.Token);

            if (embedResponse?.Embeddings is null || embedResponse.Embeddings.Count == 0)
            {
                _logger?.LogWarning("Raspunsul Ollama nu contine embeddings valide.");
                return null;
            }

            return embedResponse.Embeddings[0];
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger?.LogWarning(ex, "Apelul catre Ollama a esuat prin timeout ({Timeout} secunde).", _options.TimeoutSeconds);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Apelul catre Ollama /api/embed a esuat: {Message}", ex.Message);
            return null;
        }
    }

    private sealed class OllamaEmbedRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;
    }

    private sealed class OllamaEmbedResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("embeddings")]
        public List<float[]>? Embeddings { get; set; }
    }
}

