using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Infrastructure.Persistence.Configurations;

// EF Core configuration is infrastructure glue and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.HasMany(s => s.TutorSubjects)
            .WithOne(ts => ts.Subject)
            .HasForeignKey(ts => ts.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
