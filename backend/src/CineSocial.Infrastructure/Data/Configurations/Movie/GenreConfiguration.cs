using CineSocial.Domain.Entities.Movie;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Movie;

public class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.ToTable("Genres");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.TmdbId)
            .IsRequired();

        builder.HasIndex(g => g.TmdbId)
            .IsUnique();

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(100);
    }
}
