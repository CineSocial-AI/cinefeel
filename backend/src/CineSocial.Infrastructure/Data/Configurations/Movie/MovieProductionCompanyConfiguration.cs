using CineSocial.Domain.Entities.Movie;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Movie;

public class MovieProductionCompanyConfiguration : IEntityTypeConfiguration<MovieProductionCompany>
{
    public void Configure(EntityTypeBuilder<MovieProductionCompany> builder)
    {
        builder.ToTable("MovieProductionCompanies");

        builder.HasKey(mpc => new { mpc.MovieId, mpc.ProductionCompanyId });

        builder.HasOne(mpc => mpc.Movie)
            .WithMany(m => m.MovieProductionCompanies)
            .HasForeignKey(mpc => mpc.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mpc => mpc.ProductionCompany)
            .WithMany(pc => pc.MovieProductionCompanies)
            .HasForeignKey(mpc => mpc.ProductionCompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
