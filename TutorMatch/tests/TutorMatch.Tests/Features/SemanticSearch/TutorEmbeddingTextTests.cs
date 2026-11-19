using FluentAssertions;
using TutorMatch.Api.Features.SemanticSearch.Services;

namespace TutorMatch.Tests.Features.SemanticSearch;

public class TutorEmbeddingTextTests
{
    [Fact]
    public void Given_FullProfile_When_Building_Then_ReturnsNameSubjectsBioSeparatedByPipes()
    {
        var subjects = new[] { "Algoritmi", "C++" };

        var text = TutorEmbeddingText.Build("Alexandru Radu", subjects, "Trainer de algoritmi.");

        text.Should().Be("Alexandru Radu | Algoritmi, C++ | Trainer de algoritmi.");
    }

    [Fact]
    public void Given_UnorderedDuplicatedSubjects_When_Building_Then_SubjectsAreTrimmedDeduplicatedAndSortedIgnoringCase()
    {
        var subjects = new[] { " POO ", "algoritmi", "Algoritmi", "C++", "   ", null };

        var text = TutorEmbeddingText.Build("Ana", subjects, "Bio");

        text.Should().Be("Ana | algoritmi, C++, POO | Bio");
    }

    [Fact]
    public void Given_SameSubjectsInDifferentOrder_When_Building_Then_ProducesIdenticalText()
    {
        var first = TutorEmbeddingText.Build("Ana", new[] { "Fizica", "Chimie" }, "Bio");

        var second = TutorEmbeddingText.Build("Ana", new[] { "Chimie", "Fizica" }, "Bio");

        second.Should().Be(first);
    }

    [Fact]
    public void Given_NoSubjectsAndPaddedValues_When_Building_Then_KeepsEmptySubjectSegmentAndTrimsValues()
    {
        var text = TutorEmbeddingText.Build("  Radu  ", Array.Empty<string>(), "  Ajut la matematica.  ");

        text.Should().Be("Radu |  | Ajut la matematica.");
    }

    [Fact]
    public void Given_NullInputs_When_Building_Then_ReturnsOnlySeparators()
    {
        var text = TutorEmbeddingText.Build(null, null, null);

        text.Should().Be(" |  | ");
    }
}
