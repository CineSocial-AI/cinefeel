using CineSocial.Domain.Entities.Movie;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Movie;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Iso31661)
            .IsRequired()
            .HasMaxLength(2);

        builder.HasIndex(c => c.Iso31661)
            .IsUnique();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);
    }
}
