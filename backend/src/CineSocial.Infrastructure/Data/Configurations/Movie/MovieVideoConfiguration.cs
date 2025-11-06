using CineSocial.Domain.Entities.Movie;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Movie;

public class MovieVideoConfiguration : IEntityTypeConfiguration<MovieVideo>
{
    public void Configure(EntityTypeBuilder<MovieVideo> builder)
    {
        builder.ToTable("MovieVideos");

        builder.HasKey(mv => mv.Id);

        builder.Property(mv => mv.VideoKey)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(mv => mv.Name)
            .HasMaxLength(200);

        builder.Property(mv => mv.Site)
            .HasMaxLength(50);

        builder.Property(mv => mv.Type)
            .HasMaxLength(50);

        builder.HasOne(mv => mv.Movie)
            .WithMany(m => m.MovieVideos)
            .HasForeignKey(mv => mv.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mv => mv.MovieId);
    }
}
