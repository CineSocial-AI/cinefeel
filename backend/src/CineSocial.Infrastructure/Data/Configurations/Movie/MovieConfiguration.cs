using CineSocial.Domain.Entities.Movie;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Movie;

public class MovieConfiguration : IEntityTypeConfiguration<MovieEntity>
{
    public void Configure(EntityTypeBuilder<MovieEntity> builder)
    {
        builder.ToTable("Movies");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.TmdbId)
            .IsRequired();

        builder.HasIndex(m => m.TmdbId)
            .IsUnique();

        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(m => m.OriginalTitle)
            .HasMaxLength(500);

        builder.Property(m => m.Overview)
            .HasMaxLength(2000);

        builder.Property(m => m.ImdbId)
            .HasMaxLength(20);

        builder.Property(m => m.OriginalLanguage)
            .HasMaxLength(10);

        builder.Property(m => m.Status)
            .HasMaxLength(50);

        builder.Property(m => m.Tagline)
            .HasMaxLength(500);

        builder.Property(m => m.Homepage)
            .HasMaxLength(500);

        builder.Property(m => m.PosterPath)
            .HasMaxLength(200);

        builder.Property(m => m.BackdropPath)
            .HasMaxLength(200);

        builder.Property(m => m.Budget)
            .HasColumnType("decimal(18,2)");

        builder.Property(m => m.Revenue)
            .HasColumnType("decimal(18,2)");

        // Configure vector embedding for content-based recommendations
        // Using 384 dimensions (common for sentence transformers like all-MiniLM-L6-v2)
        builder.Property(m => m.ContentEmbedding)
            .HasColumnType("vector(384)");

        builder.HasIndex(m => m.Popularity);
        builder.HasIndex(m => m.VoteAverage);
        builder.HasIndex(m => m.ReleaseDate);
    }
}
