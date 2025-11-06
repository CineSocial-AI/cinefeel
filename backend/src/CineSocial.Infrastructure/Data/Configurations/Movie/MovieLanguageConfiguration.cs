using CineSocial.Domain.Entities.Movie;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Movie;

public class MovieLanguageConfiguration : IEntityTypeConfiguration<MovieLanguage>
{
    public void Configure(EntityTypeBuilder<MovieLanguage> builder)
    {
        builder.ToTable("MovieLanguages");

        builder.HasKey(ml => new { ml.MovieId, ml.LanguageId });

        builder.HasOne(ml => ml.Movie)
            .WithMany(m => m.MovieLanguages)
            .HasForeignKey(ml => ml.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ml => ml.Language)
            .WithMany(l => l.MovieLanguages)
            .HasForeignKey(ml => ml.LanguageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
