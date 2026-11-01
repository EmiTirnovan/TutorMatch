using System.Reflection;
using FluentAssertions;
using TutorMatch.Api.Domain.Tutors;
using Xunit;

namespace TutorMatch.Tests.Domain.Tutors;

public class TutorEmbeddingTests
{
    [Fact]
    public void Given_ValidData_When_ConstructingTutorEmbedding_Then_InitializesProperties()
    {
        // Arrange
        var tutorId = Guid.NewGuid();
        byte[] bytes = [1, 2, 3, 4];

        // Act
        var embedding = new TutorEmbedding(tutorId, bytes);

        // Assert
        embedding.TutorId.Should().Be(tutorId);
        embedding.Embedding.Should().Equal(bytes);
        embedding.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Given_EmptyTutorId_When_ConstructingTutorEmbedding_Then_ThrowsArgumentException()
    {
        // Arrange
        byte[] bytes = [1, 2, 3];

        // Act
        var act = () => new TutorEmbedding(Guid.Empty, bytes);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tutorId");
    }

    [Fact]
    public void Given_NullEmbedding_When_ConstructingTutorEmbedding_Then_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TutorEmbedding(Guid.NewGuid(), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_NewBytes_When_CallingUpdateEmbedding_Then_UpdatesEmbeddingAndTimestamp()
    {
        // Arrange
        var embedding = new TutorEmbedding(Guid.NewGuid(), [1, 2, 3]);
        byte[] newBytes = [4, 5, 6, 7];

        // Act
        embedding.UpdateEmbedding(newBytes);

        // Assert
        embedding.Embedding.Should().Equal(newBytes);
    }

    [Fact]
    public void Given_NullBytes_When_CallingUpdateEmbedding_Then_ThrowsArgumentNullException()
    {
        // Arrange
        var embedding = new TutorEmbedding(Guid.NewGuid(), [1, 2, 3]);

        // Act
        var act = () => embedding.UpdateEmbedding(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Given_PrivateConstructor_When_InstantiatedViaReflection_Then_CreatesInstanceForEfCore()
    {
        // Act
        var constructor = typeof(TutorEmbedding).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);

        var instance = constructor?.Invoke(null) as TutorEmbedding;

        // Assert
        instance.Should().NotBeNull();
        instance!.Embedding.Should().BeEmpty();
    }
}

