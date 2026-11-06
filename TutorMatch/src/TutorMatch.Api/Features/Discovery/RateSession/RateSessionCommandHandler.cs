using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Common.Cqrs;
using TutorMatch.Api.Common.Results;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Tutors;
using TutorMatch.Api.Infrastructure.Persistence;

namespace TutorMatch.Api.Features.Discovery.RateSession;

public class RateSessionCommandHandler : ICommandHandler<RateSessionCommand, Result>
{
    private readonly TutorMatchDbContext _dbContext;

    public RateSessionCommandHandler(TutorMatchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> HandleAsync(RateSessionCommand command, CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.Sessions
            .FirstOrDefaultAsync(s => s.Id == command.SessionId, cancellationToken);

        if (session is null)
        {
            return Result.Failure(Error.NotFound("Session.NotFound", "Sesiunea nu a fost găsită."));
        }

        if (session.StudentId != command.StudentId)
        {
            return Result.Failure(Error.Conflict("Session.Conflict", "Doar studentul participant poate evalua sesiunea."));
        }

        if (session.Status != SessionStatus.Completed)
        {
            return Result.Failure(Error.Conflict("Session.Conflict", "Sesiunea poate fi evaluată doar după ce a fost finalizată."));
        }

        // Verificăm dacă există deja un rating pentru această sesiune
        var ratingExists = await _dbContext.Ratings
            .AnyAsync(r => r.SessionId == command.SessionId, cancellationToken);
            
        if (ratingExists)
        {
            return Result.Failure(Error.Conflict("Rating.Conflict", "Această sesiune a fost deja evaluată."));
        }

        try
        {
            var rating = new Rating(session.Id, session.TutorId, command.StudentId, command.Score, command.Notes);
            _dbContext.Ratings.Add(rating);
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(Error.Validation("Rating.Validation", ex.Message));
        }
    }
}