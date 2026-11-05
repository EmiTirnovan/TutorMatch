using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Common;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Features.Sessions.BookSession;

public class BookSessionCommandHandler : ICommandHandler<BookSessionCommand, Result<BookSessionResponse>>
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly ISessionRepository _sessionRepository;
    private readonly ITutorRepository _tutorRepository;
    private readonly IStudentRepository _studentRepository;

    public BookSessionCommandHandler(
        ISessionRepository sessionRepository,
        ITutorRepository tutorRepository,
        IStudentRepository studentRepository)
    {
        _sessionRepository = sessionRepository;
        _tutorRepository = tutorRepository;
        _studentRepository = studentRepository;
    }

    public async Task<Result<BookSessionResponse>> HandleAsync(
        BookSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.TutorId == Guid.Empty || command.StudentId == Guid.Empty)
        {
            return Result<BookSessionResponse>.Failure(
                Error.Validation("Session.Validation", "TutorId and StudentId must be valid GUIDs."));
        }

        DateRange interval;
        try
        {
            interval = new DateRange(command.StartTime, command.EndTime);
        }
        catch (ArgumentException ex)
        {
            return Result<BookSessionResponse>.Failure(
                Error.Validation("Session.Validation", ex.Message));
        }

        var tutor = _tutorRepository.GetById(command.TutorId);
        if (tutor is null)
        {
            return Result<BookSessionResponse>.Failure(
                Error.NotFound("Tutor.NotFound", $"Tutor with ID '{command.TutorId}' was not found."));
        }

        var student = _studentRepository.GetById(command.StudentId);
        if (student is null)
        {
            return Result<BookSessionResponse>.Failure(
                Error.NotFound("Student.NotFound", $"Student with ID '{command.StudentId}' was not found."));
        }

        // Check tutor availability
        if (!tutor.IsAvailableDuring(interval))
        {
            return Result<BookSessionResponse>.Failure(
                Error.Validation("Session.Validation", "Intervalul solicitat nu se incadreaza in disponibilitatea tutorelui."));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var allSessions = _sessionRepository.GetAll();

            // R1: Tutor overlap check
            var hasTutorOverlap = allSessions.Any(s =>
                s.TutorId == command.TutorId &&
                s.Status == SessionStatus.Booked &&
                s.Interval.Overlaps(interval));

            if (hasTutorOverlap)
            {
                return Result<BookSessionResponse>.Failure(
                    Error.Conflict("Session.Conflict", "Slotul selectat se suprapune cu o altă sesiune a tutorelui."));
            }

            // Synchronize student's session collection so the domain aggregate executes its invariants
            var existingStudentSessions = allSessions.Where(s => s.StudentId == command.StudentId);
            foreach (var s in existingStudentSessions)
            {
                if (!student.Sessions.Any(existing => existing.Id == s.Id))
                {
                    student.AddSession(s);
                }
            }

            Session session;
            try
            {
                session = student.BookSession(command.TutorId, interval);
            }
            catch (ArgumentException ex)
            {
                return Result<BookSessionResponse>.Failure(
                    Error.Validation("Session.Validation", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                // R4 is a 400 with its own code so the UI can tell it apart from a slot conflict.
                if (ex.Message.Contains("2 booked sessions", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<BookSessionResponse>.Failure(
                        Error.Validation("Session.DailyLimit", ex.Message));
                }

                return Result<BookSessionResponse>.Failure(
                    Error.Conflict("Session.Conflict", ex.Message));
            }

            try
            {
                _sessionRepository.Add(session);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result<BookSessionResponse>.Failure(
                    Error.Conflict("Session.Conflict", "Slotul a fost rezervat concurent de o altă cerere."));
            }
            catch (InvalidOperationException ex)
            {
                return Result<BookSessionResponse>.Failure(
                    Error.Conflict("Session.Conflict", ex.Message));
            }

            var response = new BookSessionResponse(
                session.Id,
                session.TutorId,
                session.StudentId,
                session.StartTime,
                session.EndTime,
                session.Status,
                session.Version);

            return Result<BookSessionResponse>.Success(response);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
