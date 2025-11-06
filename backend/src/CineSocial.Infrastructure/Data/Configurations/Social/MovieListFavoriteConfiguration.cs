using CineSocial.Domain.Entities.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Social;

public class MovieListFavoriteConfiguration : IEntityTypeConfiguration<MovieListFavorite>
{
    public void Configure(EntityTypeBuilder<MovieListFavorite> builder)
    {
        builder.ToTable("MovieListFavorites");

        // Composite primary key
        builder.HasKey(mlf => new { mlf.UserId, mlf.MovieListId });

        builder.Property(mlf => mlf.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(mlf => mlf.User)
            .WithMany()
            .HasForeignKey(mlf => mlf.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mlf => mlf.MovieList)
            .WithMany(ml => ml.Favorites)
            .HasForeignKey(mlf => mlf.MovieListId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(mlf => mlf.MovieListId);
        builder.HasIndex(mlf => mlf.UserId);
    }
}
