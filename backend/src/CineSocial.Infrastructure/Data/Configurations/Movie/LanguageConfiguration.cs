using CineSocial.Domain.Entities.Movie;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Movie;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("Languages");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Iso6391)
            .IsRequired()
            .HasMaxLength(2);

        builder.HasIndex(l => l.Iso6391)
            .IsUnique();

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.EnglishName)
            .HasMaxLength(100);
    }
}
