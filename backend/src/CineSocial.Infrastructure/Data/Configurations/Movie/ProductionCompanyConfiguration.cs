using CineSocial.Domain.Entities.Movie;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Movie;

public class ProductionCompanyConfiguration : IEntityTypeConfiguration<ProductionCompany>
{
    public void Configure(EntityTypeBuilder<ProductionCompany> builder)
    {
        builder.ToTable("ProductionCompanies");

        builder.HasKey(pc => pc.Id);

        builder.Property(pc => pc.TmdbId)
            .IsRequired();

        builder.HasIndex(pc => pc.TmdbId)
            .IsUnique();

        builder.Property(pc => pc.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(pc => pc.LogoPath)
            .HasMaxLength(200);

        builder.Property(pc => pc.OriginCountry)
            .HasMaxLength(10);
    }
}
