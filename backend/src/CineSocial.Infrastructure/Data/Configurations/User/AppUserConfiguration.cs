using CineSocial.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.User;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("AppUsers");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<int>();

        // Image relationships
        builder.HasOne(u => u.ProfileImage)
            .WithMany()
            .HasForeignKey(u => u.ProfileImageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.BackgroundImage)
            .WithMany()
            .HasForeignKey(u => u.BackgroundImageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(u => u.Bio)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex(u => u.Username)
            .IsUnique();

        // Query Filter for soft delete
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
