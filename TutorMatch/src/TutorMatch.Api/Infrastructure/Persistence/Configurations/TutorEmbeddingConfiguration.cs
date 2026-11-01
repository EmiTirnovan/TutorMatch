using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorMatch.Api.Domain.Tutors;

namespace TutorMatch.Api.Infrastructure.Persistence.Configurations;

// EF Core configuration is infrastructure glue and is excluded from unit-test coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class TutorEmbeddingConfiguration : IEntityTypeConfiguration<TutorEmbedding>
{
    public void Configure(EntityTypeBuilder<TutorEmbedding> builder)
    {
        builder.ToTable("TutorEmbeddings");

        builder.HasKey(e => e.TutorId);
        builder.Property(e => e.TutorId)
            .ValueGeneratedNever();

        builder.Property(e => e.Embedding)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.HasOne(e => e.Tutor)
            .WithOne(t => t.Embedding)
            .HasForeignKey<TutorEmbedding>(e => e.TutorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

