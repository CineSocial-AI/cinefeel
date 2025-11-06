using CineSocial.Domain.Entities.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Social;

public class MovieListItemConfiguration : IEntityTypeConfiguration<MovieListItem>
{
    public void Configure(EntityTypeBuilder<MovieListItem> builder)
    {
        builder.ToTable("MovieListItems");

        builder.HasKey(mli => mli.Id);

        builder.Property(mli => mli.Order)
            .HasDefaultValue(0);

        // Relationships
        builder.HasOne(mli => mli.MovieList)
            .WithMany(ml => ml.Items)
            .HasForeignKey(mli => mli.MovieListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mli => mli.Movie)
            .WithMany()
            .HasForeignKey(mli => mli.MovieId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: One movie per list
        builder.HasIndex(mli => new { mli.MovieListId, mli.MovieId })
            .IsUnique()
            .HasDatabaseName("IX_MovieListItems_MovieListId_MovieId_Unique");

        // Index for ordering
        builder.HasIndex(mli => new { mli.MovieListId, mli.Order });
    }
}
