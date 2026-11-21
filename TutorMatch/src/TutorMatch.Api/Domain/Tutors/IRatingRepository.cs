namespace TutorMatch.Api.Domain.Tutors;

// Read-side access to ratings, used by queries that expose rating data (session lists, tutor profile).
public interface IRatingRepository
{
    IReadOnlyList<Rating> GetByTutorId(Guid tutorId);
    IReadOnlyList<Rating> GetBySessionIds(IEnumerable<Guid> sessionIds);
}
