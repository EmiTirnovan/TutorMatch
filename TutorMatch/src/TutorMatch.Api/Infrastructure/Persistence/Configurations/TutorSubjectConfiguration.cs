using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Infrastructure.Persistence.Configurations;

// EF Core configuration is infrastructure glue and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class TutorSubjectConfiguration : IEntityTypeConfiguration<TutorSubject>
{
    public void Configure(EntityTypeBuilder<TutorSubject> builder)
    {
        builder.ToTable("TutorSubjects");

        // Composite primary key
        builder.HasKey(ts => new { ts.TutorId, ts.SubjectId });

        builder.HasOne(ts => ts.Tutor)
            .WithMany(t => t.TutorSubjects)
            .HasForeignKey(ts => ts.TutorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ts => ts.Subject)
            .WithMany(s => s.TutorSubjects)
            .HasForeignKey(ts => ts.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
