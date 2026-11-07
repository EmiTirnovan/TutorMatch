using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Features.Discovery.RateSession;
using TutorMatch.Api.Infrastructure.Persistence;
using Xunit;

namespace TutorMatch.Tests.Features.Discovery.RateSession;

public class RateSessionCommandHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TutorMatchDbContext _context;
    private readonly RateSessionCommandHandler _handler;

    public RateSessionCommandHandlerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TutorMatchDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TutorMatchDbContext(options);
        _context.Database.EnsureCreated();

        _handler = new RateSessionCommandHandler(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task<Session> SeedSessionAsync(SessionStatus status = SessionStatus.Completed)
    {
        var tutor = new Tutor("Tutor Rating", $"tutor-{Guid.NewGuid():N}@test.com", "Bio", 100m);
        var student = new Student(Guid.NewGuid(), "Student Rating", $"student-{Guid.NewGuid():N}@test.com");
        var start = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);
        var session = new Session(Guid.NewGuid(), tutor.Id, student.Id, new DateRange(start, start.AddHours(1)), status);

        _context.Tutors.Add(tutor);
        _context.Students.Add(student);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return session;
    }

    private Task<List<Rating>> GetStoredRatingsAsync()
    {
        return _context.Ratings.AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task Given_NonExistentSession_When_Rating_Then_ReturnsNotFound()
    {
        var command = new RateSessionCommand(Guid.NewGuid(), Guid.NewGuid(), 5, "Great");

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("Session.NotFound");
        result.Error.Description.Should().Be("Sesiunea nu a fost găsită.");
        (await GetStoredRatingsAsync()).Should().BeEmpty();
    }

    [Theory]
    [InlineData(SessionStatus.Booked)]
    [InlineData(SessionStatus.Cancelled)]
    [InlineData(SessionStatus.LateCancelled)]
    public async Task Given_SessionNotCompleted_When_Rating_Then_ReturnsConflictAndStoresNothing(SessionStatus status)
    {
        var session = await SeedSessionAsync(status);
        var command = new RateSessionCommand(session.Id, session.StudentId, 5, "Too early");

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Session.Conflict");
        result.Error.Description.Should().Be("Sesiunea poate fi evaluată doar după ce a fost finalizată.");
        (await GetStoredRatingsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Given_NonCompletedSessionAndWrongStudent_When_Rating_Then_ReportsWrongStudentFirst()
    {
        var session = await SeedSessionAsync(SessionStatus.Booked);
        var command = new RateSessionCommand(session.Id, Guid.NewGuid(), 5, null);

        var result = await _handler.HandleAsync(command);

        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Description.Should().Be("Doar studentul participant poate evalua sesiunea.");
    }

    [Fact]
    public async Task Given_StudentWhoDidNotAttend_When_Rating_Then_ReturnsConflictAndStoresNothing()
    {
        var session = await SeedSessionAsync();
        var command = new RateSessionCommand(session.Id, Guid.NewGuid(), 4, "Not my session");

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Session.Conflict");
        result.Error.Description.Should().Be("Doar studentul participant poate evalua sesiunea.");
        (await GetStoredRatingsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Given_AlreadyRatedSession_When_RatingAgain_Then_ReturnsConflictAndKeepsOriginalRating()
    {
        var session = await SeedSessionAsync();
        await _handler.HandleAsync(new RateSessionCommand(session.Id, session.StudentId, 5, "First"));
        var secondCommand = new RateSessionCommand(session.Id, session.StudentId, 1, "Second");

        var result = await _handler.HandleAsync(secondCommand);

        var stored = await GetStoredRatingsAsync();
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Rating.Conflict");
        result.Error.Description.Should().Be("Această sesiune a fost deja evaluată.");
        stored.Should().ContainSingle();
        stored[0].Score.Should().Be(5);
        stored[0].Notes.Should().Be("First");
    }

    [Fact]
    public async Task Given_ParticipatingStudent_When_Rating_Then_StoresRatingLinkedToSessionTutorAndStudent()
    {
        var session = await SeedSessionAsync();
        var command = new RateSessionCommand(session.Id, session.StudentId, 4, "Very clear");

        var result = await _handler.HandleAsync(command);

        var stored = await GetStoredRatingsAsync();
        result.IsSuccess.Should().BeTrue();
        stored.Should().ContainSingle();
        stored[0].SessionId.Should().Be(session.Id);
        stored[0].TutorId.Should().Be(session.TutorId);
        stored[0].StudentId.Should().Be(session.StudentId);
        stored[0].Score.Should().Be(4);
        stored[0].Notes.Should().Be("Very clear");
    }

    [Fact]
    public async Task Given_NotesWithSurroundingWhitespace_When_Rating_Then_StoresTrimmedNotes()
    {
        var session = await SeedSessionAsync();
        var command = new RateSessionCommand(session.Id, session.StudentId, 5, "   Excellent   ");

        var result = await _handler.HandleAsync(command);

        var stored = await GetStoredRatingsAsync();
        result.IsSuccess.Should().BeTrue();
        stored[0].Notes.Should().Be("Excellent");
    }

    [Fact]
    public async Task Given_NoNotes_When_Rating_Then_StoresRatingWithoutNotes()
    {
        var session = await SeedSessionAsync();
        var command = new RateSessionCommand(session.Id, session.StudentId, 3, null);

        var result = await _handler.HandleAsync(command);

        var stored = await GetStoredRatingsAsync();
        result.IsSuccess.Should().BeTrue();
        stored.Should().ContainSingle();
        stored[0].Notes.Should().BeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task Given_ScoreWithinRange_When_Rating_Then_ReturnsSuccess(int score)
    {
        var session = await SeedSessionAsync();
        var command = new RateSessionCommand(session.Id, session.StudentId, score, null);

        var result = await _handler.HandleAsync(command);

        var stored = await GetStoredRatingsAsync();
        result.IsSuccess.Should().BeTrue();
        stored.Should().ContainSingle();
        stored[0].Score.Should().Be(score);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task Given_ScoreOutsideRange_When_Rating_Then_ReturnsValidationErrorAndStoresNothing(int score)
    {
        var session = await SeedSessionAsync();
        var command = new RateSessionCommand(session.Id, session.StudentId, score, "Invalid");

        var result = await _handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("Rating.Validation");
        result.Error.Description.Should().Contain("Scorul trebuie să fie intre 1 și 5.");
        (await GetStoredRatingsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Given_RatedSession_When_RatingAnotherSession_Then_ReturnsSuccess()
    {
        var firstSession = await SeedSessionAsync();
        var secondSession = await SeedSessionAsync();
        await _handler.HandleAsync(new RateSessionCommand(firstSession.Id, firstSession.StudentId, 5, null));

        var result = await _handler.HandleAsync(new RateSessionCommand(secondSession.Id, secondSession.StudentId, 2, null));

        var stored = await GetStoredRatingsAsync();
        result.IsSuccess.Should().BeTrue();
        stored.Should().HaveCount(2);
    }
}
