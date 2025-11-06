using CineSocial.Domain.Entities.Movie;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Movie;

public class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> builder)
    {
        builder.ToTable("Collections");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TmdbId)
            .IsRequired();

        builder.HasIndex(c => c.TmdbId)
            .IsUnique();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Overview)
            .HasMaxLength(2000);

        builder.Property(c => c.PosterPath)
            .HasMaxLength(200);

        builder.Property(c => c.BackdropPath)
            .HasMaxLength(200);
    }
}
