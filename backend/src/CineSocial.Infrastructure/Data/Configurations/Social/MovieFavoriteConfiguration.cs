using CineSocial.Domain.Entities.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Social;

public class MovieFavoriteConfiguration : IEntityTypeConfiguration<MovieFavorite>
{
    public void Configure(EntityTypeBuilder<MovieFavorite> builder)
    {
        builder.ToTable("MovieFavorites");

        builder.HasKey(mf => mf.Id);

        builder.HasOne(mf => mf.User)
            .WithMany()
            .HasForeignKey(mf => mf.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(mf => mf.Movie)
            .WithMany()
            .HasForeignKey(mf => mf.MovieId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(mf => new { mf.UserId, mf.MovieId })
            .IsUnique();

        builder.HasQueryFilter(mf => !mf.IsDeleted);
    }
}
