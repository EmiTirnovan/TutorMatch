using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Infrastructure.Persistence.Configurations;

// EF Core configuration is infrastructure glue and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class TutorConfiguration : IEntityTypeConfiguration<Tutor>
{
    public void Configure(EntityTypeBuilder<Tutor> builder)
    {
        builder.ToTable("Tutors");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Bio)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(t => t.HourlyRate)
            .HasPrecision(18, 2)
            .IsRequired();

        // Map collection of Value Objects (DateRange) to TutorAvailabilities owned table
        builder.OwnsMany(t => t.Availability, availabilityBuilder =>
        {
            availabilityBuilder.ToTable("TutorAvailabilities");

            availabilityBuilder.WithOwner()
                .HasForeignKey("TutorId");

            availabilityBuilder.Property<Guid>("Id")
                .ValueGeneratedOnAdd();

            availabilityBuilder.HasKey("Id");

            availabilityBuilder.Property(d => d.Start)
                .IsRequired();

            availabilityBuilder.Property(d => d.End)
                .IsRequired();
        });

        // Relationship with TutorSubject join entity
        builder.HasMany(t => t.TutorSubjects)
            .WithOne(ts => ts.Tutor)
            .HasForeignKey(ts => ts.TutorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
