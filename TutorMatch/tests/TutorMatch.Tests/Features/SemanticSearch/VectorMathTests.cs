using FluentAssertions;
using TutorMatch.Api.Features.SemanticSearch.Services;
using Xunit;

namespace TutorMatch.Tests.Features.SemanticSearch;

public class VectorMathTests
{
    [Fact]
    public void Given_IdenticalVectors_When_CalculatingCosineSimilarity_Then_ReturnsOne()
    {
        // Arrange
        float[] a = [1.0f, 2.0f, 3.0f, 4.0f];
        float[] b = [1.0f, 2.0f, 3.0f, 4.0f];

        // Act
        float score = VectorMath.CosineSimilarity(a, b);

        // Assert
        score.Should().BeApproximately(1.0f, 1e-5f);
    }

    [Fact]
    public void Given_ProportionalPositiveVectors_When_CalculatingCosineSimilarity_Then_ReturnsOne()
    {
        // Arrange
        float[] a = [1.0f, 2.0f, 3.0f];
        float[] b = [2.0f, 4.0f, 6.0f];

        // Act
        float score = VectorMath.CosineSimilarity(a, b);

        // Assert
        score.Should().BeApproximately(1.0f, 1e-5f);
    }

    [Fact]
    public void Given_OrthogonalVectors_When_CalculatingCosineSimilarity_Then_ReturnsZero()
    {
        // Arrange
        float[] a = [1.0f, 0.0f];
        float[] b = [0.0f, 1.0f];

        // Act
        float score = VectorMath.CosineSimilarity(a, b);

        // Assert
        score.Should().BeApproximately(0.0f, 1e-5f);
    }

    [Fact]
    public void Given_OrthogonalVectors3D_When_CalculatingCosineSimilarity_Then_ReturnsZero()
    {
        // Arrange
        float[] a = [1.0f, 0.0f, 0.0f];
        float[] b = [0.0f, 0.0f, 1.0f];

        // Act
        float score = VectorMath.CosineSimilarity(a, b);

        // Assert
        score.Should().BeApproximately(0.0f, 1e-5f);
    }

    [Fact]
    public void Given_OppositeVectors_When_CalculatingCosineSimilarity_Then_ReturnsMinusOne()
    {
        // Arrange
        float[] a = [1.0f, -2.0f, 3.0f];
        float[] b = [-1.0f, 2.0f, -3.0f];

        // Act
        float score = VectorMath.CosineSimilarity(a, b);

        // Assert
        score.Should().BeApproximately(-1.0f, 1e-5f);
    }

    [Fact]
    public void Given_ZeroVector_When_CalculatingCosineSimilarity_Then_ReturnsZeroWithoutDivisionByZeroException()
    {
        // Arrange
        float[] a = [0.0f, 0.0f, 0.0f];
        float[] b = [1.0f, 2.0f, 3.0f];

        // Act
        float score = VectorMath.CosineSimilarity(a, b);

        // Assert
        score.Should().Be(0.0f);
    }

    [Fact]
    public void Given_BothZeroVectors_When_CalculatingCosineSimilarity_Then_ReturnsZero()
    {
        // Arrange
        float[] a = [0.0f, 0.0f];
        float[] b = [0.0f, 0.0f];

        // Act
        float score = VectorMath.CosineSimilarity(a, b);

        // Assert
        score.Should().Be(0.0f);
    }

    [Fact]
    public void Given_DifferentLengthVectors_When_CalculatingCosineSimilarity_Then_ReturnsZero()
    {
        // Arrange
        float[] a = [1.0f, 2.0f];
        float[] b = [1.0f, 2.0f, 3.0f];

        // Act
        float score = VectorMath.CosineSimilarity(a, b);

        // Assert
        score.Should().Be(0.0f);
    }

    [Fact]
    public void Given_NullOrEmptyVectors_When_CalculatingCosineSimilarity_Then_ReturnsZero()
    {
        // Act & Assert
        VectorMath.CosineSimilarity(null, [1.0f]).Should().Be(0.0f);
        VectorMath.CosineSimilarity([1.0f], null).Should().Be(0.0f);
        VectorMath.CosineSimilarity([], [1.0f]).Should().Be(0.0f);
        VectorMath.CosineSimilarity([1.0f], []).Should().Be(0.0f);
    }

    [Fact]
    public void Given_ValidFloatVector_When_SerializingAndDeserializing_Then_RoundTripIsExact()
    {
        // Arrange
        float[] original = [0.123456f, -987.654f, 0.0f, 1e-4f, 3.1415927f];

        // Act
        byte[] bytes = VectorMath.Serialize(original);
        float[] deserialized = VectorMath.Deserialize(bytes);

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().Be(original.Length * sizeof(float));
        deserialized.Should().Equal(original);
    }

    [Fact]
    public void Given_NullOrEmptyVector_When_Serializing_Then_ReturnsEmptyByteArray()
    {
        // Act & Assert
        VectorMath.Serialize(null).Should().BeEmpty();
        VectorMath.Serialize([]).Should().BeEmpty();
    }

    [Fact]
    public void Given_NullOrEmptyByteArray_When_Deserializing_Then_ReturnsEmptyFloatArray()
    {
        // Act & Assert
        VectorMath.Deserialize(null).Should().BeEmpty();
        VectorMath.Deserialize([]).Should().BeEmpty();
    }
}

