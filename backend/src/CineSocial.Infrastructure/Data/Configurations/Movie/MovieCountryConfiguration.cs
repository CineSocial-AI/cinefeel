using CineSocial.Domain.Entities.Movie;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Movie;

public class MovieCountryConfiguration : IEntityTypeConfiguration<MovieCountry>
{
    public void Configure(EntityTypeBuilder<MovieCountry> builder)
    {
        builder.ToTable("MovieCountries");

        builder.HasKey(mc => new { mc.MovieId, mc.CountryId });

        builder.HasOne(mc => mc.Movie)
            .WithMany(m => m.MovieCountries)
            .HasForeignKey(mc => mc.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mc => mc.Country)
            .WithMany(c => c.MovieCountries)
            .HasForeignKey(mc => mc.CountryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
