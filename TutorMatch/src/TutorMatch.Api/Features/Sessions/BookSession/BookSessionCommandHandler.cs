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
    private readonly IMeetingService _meetingService;

    public BookSessionCommandHandler(
        ISessionRepository sessionRepository,
        ITutorRepository tutorRepository,
        IStudentRepository studentRepository,
        IMeetingService meetingService)
    {
        _meetingService = meetingService;
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

        if (command.Type == SessionType.Physical && string.IsNullOrWhiteSpace(command.LocationName))
        {
            return Result<BookSessionResponse>.Failure(
                Error.Validation("Session.Validation", "LocationName is required for physical sessions."));
        }

        // The frontend pins every physical address on the map; requiring the pin here stops API callers skipping it.
        if (command.Type == SessionType.Physical && (command.Latitude is null || command.Longitude is null))
        {
            return Result<BookSessionResponse>.Failure(
                Error.Validation("Session.Validation", "Latitude and Longitude are required for physical sessions."));
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

        Session session;
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

            try
            {
                session = student.BookSession(
                    command.TutorId,
                    interval,
                    command.Subject,
                    command.Notes,
                    command.Type,
                    null, // MeetingLink is attached after the session is saved
                    command.LocationName,
                    command.Latitude,
                    command.Longitude);
            }
            catch (ArgumentException ex)
            {
                return Result<BookSessionResponse>.Failure(
                    Error.Validation("Session.Validation", ex.Message));
            }
            catch (DailySessionLimitExceededException ex)
            {
                // R4 is a 400 with its own code so the UI can tell it apart from a slot conflict.
                return Result<BookSessionResponse>.Failure(
                    Error.Validation("Session.DailyLimit", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
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
        }
        finally
        {
            _semaphore.Release();
        }

        // Outside the lock: a slow external call must not block other bookings.
        if (session.Type == SessionType.Online)
        {
            var linkError = await AttachMeetingLinkAsync(session);
            if (linkError is not null)
            {
                return Result<BookSessionResponse>.Failure(linkError);
            }
        }

        return Result<BookSessionResponse>.Success(new BookSessionResponse(
            session.Id,
            session.TutorId,
            session.StudentId,
            session.StartTime,
            session.EndTime,
            session.Status,
            session.Version,
            session.Subject,
            session.Notes,
            session.Type,
            session.MeetingLink,
            session.LocationName,
            session.Latitude,
            session.Longitude));
    }

    private async Task<Error?> AttachMeetingLinkAsync(Session session)
    {
        try
        {
            var topic = string.IsNullOrWhiteSpace(session.Subject) ? "Sesiune TutorMatch" : $"TutorMatch: {session.Subject}";
            var link = await _meetingService.GenerateMeetingLinkAsync(session.Id, topic, session.StartTime, session.EndTime);
            session.AttachMeetingLink(link);
            _sessionRepository.Update(session);
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or ArgumentException)
        {
            // An online session without a link is unusable: undo the booking so the slot is freed.
            _sessionRepository.Delete(session.Id);
            return Error.Failure("Session.MeetingLinkFailed", "Nu s-a putut genera link-ul de meeting. Rezervarea a fost anulata, incearca din nou.");
        }
    }
}
