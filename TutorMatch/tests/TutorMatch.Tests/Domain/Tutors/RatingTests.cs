using FluentAssertions;
using TutorMatch.Api.Domain.Tutors;
using Xunit;

namespace TutorMatch.Tests.Domain.Tutors;

public class RatingTests
{
    [Fact]
    public void Given_ValidData_When_ConstructingRating_Then_InitializesValues()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var tutorId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        
        // Act
        var rating = new Rating(sessionId, tutorId, studentId, 5, "Excelent!");
        
        // Assert
        rating.SessionId.Should().Be(sessionId);
        rating.TutorId.Should().Be(tutorId);
        rating.StudentId.Should().Be(studentId);
        rating.Score.Should().Be(5);
        rating.Notes.Should().Be("Excelent!");
        rating.Id.Should().NotBeEmpty();
        rating.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Given_InvalidScore_When_ConstructingRating_Then_ThrowsArgumentException(int invalidScore)
    {
        // Act
        var action = () => new Rating(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), invalidScore);
        
        // Assert
        action.Should().Throw<ArgumentException>().WithMessage("*Scorul trebuie să fie intre 1 și 5*");
    }

    [Fact]
    public void Given_EmptyGuids_When_ConstructingRating_Then_ThrowsArgumentException()
    {
        // Act
        var action = () => new Rating(Guid.Empty, Guid.Empty, Guid.Empty, 5);
        
        // Assert
        action.Should().Throw<ArgumentException>();
    }
}