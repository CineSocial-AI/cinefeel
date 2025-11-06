using CineSocial.Domain.Entities.Movie;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Movie;

public class MovieCastConfiguration : IEntityTypeConfiguration<MovieCast>
{
    public void Configure(EntityTypeBuilder<MovieCast> builder)
    {
        builder.ToTable("MovieCast");

        builder.HasKey(mc => mc.Id);

        builder.Property(mc => mc.Character)
            .HasMaxLength(500);

        builder.HasOne(mc => mc.Movie)
            .WithMany(m => m.MovieCasts)
            .HasForeignKey(mc => mc.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mc => mc.Person)
            .WithMany(p => p.MovieCasts)
            .HasForeignKey(mc => mc.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mc => mc.MovieId);
        builder.HasIndex(mc => mc.PersonId);
    }
}
