using CineSocial.Domain.Entities.Movie;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Movie;

public class MovieCollectionConfiguration : IEntityTypeConfiguration<MovieCollection>
{
    public void Configure(EntityTypeBuilder<MovieCollection> builder)
    {
        builder.ToTable("MovieCollections");

        builder.HasKey(mc => new { mc.MovieId, mc.CollectionId });

        builder.HasOne(mc => mc.Movie)
            .WithMany(m => m.MovieCollections)
            .HasForeignKey(mc => mc.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mc => mc.Collection)
            .WithMany(c => c.MovieCollections)
            .HasForeignKey(mc => mc.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
