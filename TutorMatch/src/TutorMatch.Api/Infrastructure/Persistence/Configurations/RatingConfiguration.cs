using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Infrastructure.Persistence.Configurations;

// EF Core configuration is infrastructure glue and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> builder)
    {
        builder.ToTable("Ratings");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.SessionId)
            .IsRequired();

        builder.Property(r => r.TutorId)
            .IsRequired();

        builder.Property(r => r.StudentId)
            .IsRequired();

        builder.Property(r => r.Score)
            .IsRequired();

        builder.Property(r => r.Notes)
            .HasMaxLength(500);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne<Session>()
            .WithMany()
            .HasForeignKey(r => r.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tutor>()
            .WithMany()
            .HasForeignKey(r => r.TutorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
