using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TutorMatch.Tests.Features.SemanticSearch.Fakes;

/// <summary>
/// Offline stand-in for an embedding model: a hashed bag-of-words vector over lowercased,
/// diacritic-free tokens truncated to 6 characters (crude stemming). It is deterministic across
/// processes (FNV-1a, not string.GetHashCode) and captures lexical overlap only, not meaning.
/// </summary>
public static class DeterministicTextEmbedder
{
    public const int Dimensions = 512;
    private const int StemLength = 6;

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "and", "are", "care", "caut", "cel", "cea", "cineva", "cum", "din", "este", "for", "foarte",
        "imi", "in", "la", "mai", "nevoie", "pentru", "pana", "sau", "sunt", "the", "vreau", "with",
        "ajut", "ajutor", "meditatii", "mea", "meu", "nu", "pe", "si", "sa", "de", "cu", "ma", "am"
    };

    public static float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        foreach (var token in Tokenize(text))
        {
            vector[Fnv1a(token) % Dimensions] += 1f;
        }

        return vector;
    }

    public static IEnumerable<string> Tokenize(string text)
    {
        var normalized = RemoveDiacritics((text ?? string.Empty).ToLowerInvariant());
        return Regex.Split(normalized, "[^a-z0-9+#]+")
            .Where(token => token.Length >= 3 && !StopWords.Contains(token))
            .Select(token => token.Length > StemLength ? token[..StemLength] : token);
    }

    private static string RemoveDiacritics(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static uint Fnv1a(string token)
    {
        var hash = 2166136261u;
        foreach (var character in token)
        {
            hash ^= character;
            hash *= 16777619u;
        }

        return hash;
    }
}
