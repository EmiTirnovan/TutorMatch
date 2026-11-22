namespace TutorMatch.Api.Features.SemanticSearch.Services;

/// <summary>
/// Builds the text sent to the embedding model for a tutor profile.
/// Format: <c>Name + " | " + Subjects + " | " + Bio</c>, where subjects are trimmed,
/// de-duplicated (case-insensitive) and sorted so the same profile always yields the same text.
/// </summary>
public static class TutorEmbeddingText
{
    public const string SegmentSeparator = " | ";
    public const string SubjectSeparator = ", ";

    public static string Build(string? name, IEnumerable<string?>? subjects, string? bio)
    {
        var normalizedSubjects = (subjects ?? Enumerable.Empty<string?>())
            .Where(subject => !string.IsNullOrWhiteSpace(subject))
            .Select(subject => subject!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase);

        return string.Concat(
            name?.Trim() ?? string.Empty,
            SegmentSeparator,
            string.Join(SubjectSeparator, normalizedSubjects),
            SegmentSeparator,
            bio?.Trim() ?? string.Empty);
    }
}
