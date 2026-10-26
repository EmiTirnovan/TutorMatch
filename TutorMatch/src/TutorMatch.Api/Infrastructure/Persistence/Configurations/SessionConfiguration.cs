using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorMatch.Api.Domain.Sessions;
using TutorMatch.Api.Domain.Students;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Infrastructure.Persistence.Configurations;

// EF Core configuration is infrastructure glue and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.TutorId)
            .IsRequired();

        builder.Property(s => s.StudentId)
            .IsRequired();

        // Map Value Object Interval (DateRange) with embedded columns StartTime and EndTime
        builder.OwnsOne(s => s.Interval, intervalBuilder =>
        {
            intervalBuilder.Property(d => d.Start)
                .HasColumnName("StartTime")
                .IsRequired();

            intervalBuilder.Property(d => d.End)
                .HasColumnName("EndTime")
                .IsRequired();
        });

        // Ignore computed convenience properties that reference Interval
        builder.Ignore(s => s.DateRange);
        builder.Ignore(s => s.StartTime);
        builder.Ignore(s => s.EndTime);

        // Map status enum to string for clarity in SQLite
        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Concurrency token for optimistic concurrency control
        builder.Property(s => s.Version)
            .IsConcurrencyToken()
            .IsRequired();

        // Relationships
        builder.HasOne<Tutor>()
            .WithMany()
            .HasForeignKey(s => s.TutorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Student>()
            .WithMany(st => st.Sessions)
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
