using CineSocial.Domain.Entities.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Social;

public class RateConfiguration : IEntityTypeConfiguration<Rate>
{
    public void Configure(EntityTypeBuilder<Rate> builder)
    {
        builder.ToTable("Rates");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating)
            .IsRequired()
            .HasPrecision(3, 1);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .IsRequired();

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.UserId, r.MovieId })
            .IsUnique();

        builder.HasIndex(r => r.MovieId);
    }
}
