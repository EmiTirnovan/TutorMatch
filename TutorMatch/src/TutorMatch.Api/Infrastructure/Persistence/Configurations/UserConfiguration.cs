using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorMatch.Api.Domain.Users;

namespace TutorMatch.Api.Infrastructure.Persistence.Configurations;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.GoogleId)
            .HasMaxLength(200);

        builder.HasIndex(u => u.GoogleId);

        builder.Property(u => u.CreatedAt)
            .IsRequired();
    }
}
