using CineSocial.Domain.Entities.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Social;

public class MovieListConfiguration : IEntityTypeConfiguration<MovieList>
{
    public void Configure(EntityTypeBuilder<MovieList> builder)
    {
        builder.ToTable("MovieLists");

        builder.HasKey(ml => ml.Id);

        builder.Property(ml => ml.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ml => ml.Description)
            .HasMaxLength(1000);

        builder.Property(ml => ml.IsPublic)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(ml => ml.IsWatchlist)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ml => ml.FavoriteCount)
            .HasDefaultValue(0);

        // Relationships
        builder.HasOne(ml => ml.User)
            .WithMany()
            .HasForeignKey(ml => ml.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ml => ml.Items)
            .WithOne(mli => mli.MovieList)
            .HasForeignKey(mli => mli.MovieListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ml => ml.Favorites)
            .WithOne(mlf => mlf.MovieList)
            .HasForeignKey(mlf => mlf.MovieListId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ml => ml.UserId);
        builder.HasIndex(ml => ml.IsPublic);
        builder.HasIndex(ml => new { ml.UserId, ml.IsWatchlist });
    }
}
