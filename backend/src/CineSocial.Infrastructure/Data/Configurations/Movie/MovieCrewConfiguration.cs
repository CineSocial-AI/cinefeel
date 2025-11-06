using CineSocial.Domain.Entities.Movie;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Movie;

public class MovieCrewConfiguration : IEntityTypeConfiguration<MovieCrew>
{
    public void Configure(EntityTypeBuilder<MovieCrew> builder)
    {
        builder.ToTable("MovieCrew");

        builder.HasKey(mc => mc.Id);

        builder.Property(mc => mc.Job)
            .HasMaxLength(200);

        builder.Property(mc => mc.Department)
            .HasMaxLength(200);

        builder.HasOne(mc => mc.Movie)
            .WithMany(m => m.MovieCrews)
            .HasForeignKey(mc => mc.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mc => mc.Person)
            .WithMany(p => p.MovieCrews)
            .HasForeignKey(mc => mc.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mc => mc.MovieId);
        builder.HasIndex(mc => mc.PersonId);
    }
}
