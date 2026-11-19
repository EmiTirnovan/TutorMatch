using System.Globalization;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TutorMatch.Api.Features.SemanticSearch.Services;
using Xunit.Abstractions;

namespace TutorMatch.Tests.Evaluation;

/// <summary>Runs only when TUTORMATCH_LIVE_EVAL=1, so the regular `dotnet test` never needs a local Ollama.</summary>
public sealed class LiveOllamaFactAttribute : FactAttribute
{
    public const string EnvironmentVariable = "TUTORMATCH_LIVE_EVAL";

    public LiveOllamaFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(EnvironmentVariable) != "1")
        {
            Skip = $"Live Ollama benchmark disabled. Set {EnvironmentVariable}=1 (and run `ollama serve`) to execute it.";
        }
    }
}

/// <summary>
/// Live benchmark against a local Ollama: for every model, indexes the evaluation tutor pool with the
/// production indexing path, runs the 30 ground-truth queries through the production SemanticSearchQueryHandler
/// (3.5 s budget, no query cache) and compares with the classic keyword search.
/// Results are written to docs/ai-evaluation-live-results.md (override with TUTORMATCH_EVAL_OUTPUT).
/// Environment: OLLAMA_BASE_URL (default http://localhost:11434), TUTORMATCH_EVAL_MODELS (default "bge-m3,nomic-embed-text").
/// </summary>
public class SemanticSearchLiveBenchmarkTests
{
    private const string DefaultModels = "bge-m3,nomic-embed-text";
    private readonly ITestOutputHelper _output;

    public SemanticSearchLiveBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [LiveOllamaFact]
    public async Task Given_LocalOllama_When_RunningBenchmark_Then_WritesComparativeReportForEveryModel()
    {
        var baseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://localhost:11434";
        var models = (Environment.GetEnvironmentVariable("TUTORMATCH_EVAL_MODELS") ?? DefaultModels)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var outputPath = Environment.GetEnvironmentVariable("TUTORMATCH_EVAL_OUTPUT")
            ?? Path.Combine(RepositoryPaths.Docs, "ai-evaluation-live-results.md");

        var report = await RunBenchmarkAsync(baseUrl, models);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, report);
        _output.WriteLine(report);

        report.Should().Contain("| Strategy |");
    }

    private static async Task<string> RunBenchmarkAsync(string baseUrl, IReadOnlyList<string> models)
    {
        var runs = new List<EvaluationRun>();
        var notes = new List<string>();
        EvaluationRun? keyword = null;

        foreach (var model in models)
        {
            // Indexing gets a generous timeout (cold model load); queries keep the production 3.5 s budget.
            var client = CreateClient(baseUrl, model, timeoutSeconds: 120);
            var warmUp = await client.GenerateEmbeddingAsync("warm-up");
            if (warmUp is null)
            {
                notes.Add($"- `{model}`: no embedding returned (is the model pulled? `ollama pull {model}`). Skipped.");
                continue;
            }

            await using var database = await EvaluationDatabase.CreateAsync(client);
            var indexed = await database.CountIndexedTutorsAsync();
            notes.Add($"- `{model}`: {warmUp.Length} dimensions, {indexed}/{SemanticSearchEvaluationTests.MockTutorPool.Count} tutors indexed.");

            var options = new OllamaOptions { BaseUrl = baseUrl, Model = model };
            runs.Add(await SemanticSearchEvaluator.RunAsync(
                $"Semantic ({model})", SemanticSearchEvaluationTests.GroundTruthEvaluationSet, database.SemanticStrategy(client, options)));

            keyword ??= await SemanticSearchEvaluator.RunAsync(
                SemanticSearchEvaluationTests.KeywordStrategy, SemanticSearchEvaluationTests.GroundTruthEvaluationSet, database.KeywordStrategy());
        }

        var allRuns = keyword is null ? runs : runs.Append(keyword).ToList();
        var builder = new StringBuilder();
        builder.AppendLine("# AI evaluation: live results");
        builder.AppendLine();
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC against `{baseUrl}`. Query budget {OllamaOptions.DefaultQueryTimeoutMilliseconds} ms, no query cache."));
        builder.AppendLine();
        notes.ForEach(note => builder.AppendLine(note));
        builder.AppendLine();
        builder.AppendLine(SemanticSearchEvaluator.ToMarkdownTable(allRuns.Select(r => r.Metrics)));
        builder.AppendLine("## Rank of the expected tutor per query");
        builder.AppendLine();
        builder.AppendLine(SemanticSearchEvaluator.ToPerCaseMarkdownTable(allRuns));
        return builder.ToString();
    }

    private static OllamaClient CreateClient(string baseUrl, string model, int timeoutSeconds)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ollama:BaseUrl"] = baseUrl,
                ["Ollama:Model"] = model,
                ["Ollama:TimeoutSeconds"] = timeoutSeconds.ToString(CultureInfo.InvariantCulture)
            })
            .Build();

        return new OllamaClient(new SingleHttpClientFactory(), configuration, Options.Create(new OllamaOptions()));
    }

    private sealed class SingleHttpClientFactory : IHttpClientFactory
    {
        private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(5) };

        public HttpClient CreateClient(string name) => Client;
    }
}
