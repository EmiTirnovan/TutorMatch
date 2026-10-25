using Microsoft.EntityFrameworkCore;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Infrastructure.Persistence;

// Dedicated Entity Framework Core DbContext for TutorMatch SQLite database
public class TutorMatchDbContext : DbContext
{
    public TutorMatchDbContext(DbContextOptions<TutorMatchDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tutor> Tutors => Set<Tutor>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TutorSubject> TutorSubjects => Set<TutorSubject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically discover and apply all IEntityTypeConfiguration implementations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TutorMatchDbContext).Assembly);
    }
}
