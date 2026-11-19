using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace TutorMatch.Tests.Evaluation;

/// <summary>What a search strategy returned for one query: ranked tutor ids and whether it degraded to keyword fallback.</summary>
public sealed record RankedSearchResult(IReadOnlyList<Guid> TutorIds, bool IsFallback);

/// <summary>Outcome of one evaluation case. <see cref="Rank"/> is 1-based, null when the expected tutor was not returned.</summary>
public sealed record EvaluationCaseOutcome(
    SemanticSearchEvaluationTests.EvaluationPair Case,
    int? Rank,
    bool IsFallback,
    double LatencyMs);

public sealed record EvaluationMetrics(
    string Strategy,
    int Total,
    double Top1Accuracy,
    double Top3Accuracy,
    double FallbackRate,
    double MeanLatencyMs);

public sealed record EvaluationRun(EvaluationMetrics Metrics, IReadOnlyList<EvaluationCaseOutcome> Outcomes);

/// <summary>
/// Runs the ground-truth set through a search strategy and computes Top-1 / Top-3 accuracy,
/// fallback rate and mean latency. Accuracy is end-to-end: when a query falls back to keyword search,
/// whatever the fallback returned is what gets scored (that is what the student would see).
/// </summary>
public static class SemanticSearchEvaluator
{
    public static async Task<EvaluationRun> RunAsync(
        string strategy,
        IReadOnlyList<SemanticSearchEvaluationTests.EvaluationPair> cases,
        Func<string, CancellationToken, Task<RankedSearchResult>> search,
        CancellationToken cancellationToken = default)
    {
        var outcomes = new List<EvaluationCaseOutcome>(cases.Count);

        foreach (var testCase in cases)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await search(testCase.NaturalLanguageQuery, cancellationToken);
            stopwatch.Stop();

            var index = result.TutorIds.ToList().IndexOf(testCase.ExpectedTutorId);
            outcomes.Add(new EvaluationCaseOutcome(
                testCase,
                index >= 0 ? index + 1 : null,
                result.IsFallback,
                stopwatch.Elapsed.TotalMilliseconds));
        }

        return new EvaluationRun(Summarize(strategy, outcomes), outcomes);
    }

    public static EvaluationMetrics Summarize(string strategy, IReadOnlyList<EvaluationCaseOutcome> outcomes)
    {
        if (outcomes.Count == 0)
        {
            return new EvaluationMetrics(strategy, 0, 0, 0, 0, 0);
        }

        double Percent(int count) => Math.Round(count * 100.0 / outcomes.Count, 1);

        return new EvaluationMetrics(
            strategy,
            outcomes.Count,
            Percent(outcomes.Count(o => o.Rank == 1)),
            Percent(outcomes.Count(o => o.Rank is >= 1 and <= 3)),
            Percent(outcomes.Count(o => o.IsFallback)),
            Math.Round(outcomes.Average(o => o.LatencyMs), 1));
    }

    public static string ToMarkdownTable(IEnumerable<EvaluationMetrics> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("| Strategy | Queries | Top-1 Accuracy | Top-3 Accuracy | Fallback Rate | Mean latency / query |");
        builder.AppendLine("|---|---:|---:|---:|---:|---:|");
        foreach (var row in rows)
        {
            builder.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"| {row.Strategy} | {row.Total} | {row.Top1Accuracy:0.0}% | {row.Top3Accuracy:0.0}% | {row.FallbackRate:0.0}% | {row.MeanLatencyMs:0.0} ms |"));
        }

        return builder.ToString();
    }

    public static string ToPerCaseMarkdownTable(IReadOnlyList<EvaluationRun> runs)
    {
        var builder = new StringBuilder();
        builder.AppendLine("| # | Query | Expected tutor | " + string.Join(" | ", runs.Select(r => r.Metrics.Strategy)) + " |");
        builder.AppendLine("|---:|---|---|" + string.Concat(runs.Select(_ => "---:|")));

        var caseCount = runs.Count == 0 ? 0 : runs[0].Outcomes.Count;
        for (var i = 0; i < caseCount; i++)
        {
            var testCase = runs[0].Outcomes[i].Case;
            var cells = runs.Select(r => FormatRank(r.Outcomes[i]));
            builder.AppendLine($"| {i + 1} | {testCase.NaturalLanguageQuery} | {testCase.ExpectedTutorName} | {string.Join(" | ", cells)} |");
        }

        return builder.ToString();
    }

    private static string FormatRank(EvaluationCaseOutcome outcome)
    {
        var rank = outcome.Rank is null ? "miss" : $"#{outcome.Rank}";
        return outcome.IsFallback ? $"{rank} (fallback)" : rank;
    }
}
