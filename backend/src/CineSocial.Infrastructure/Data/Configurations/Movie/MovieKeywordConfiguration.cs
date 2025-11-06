using CineSocial.Domain.Entities.Movie;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Movie;

public class MovieKeywordConfiguration : IEntityTypeConfiguration<MovieKeyword>
{
    public void Configure(EntityTypeBuilder<MovieKeyword> builder)
    {
        builder.ToTable("MovieKeywords");

        builder.HasKey(mk => new { mk.MovieId, mk.KeywordId });

        builder.HasOne(mk => mk.Movie)
            .WithMany(m => m.MovieKeywords)
            .HasForeignKey(mk => mk.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mk => mk.Keyword)
            .WithMany(k => k.MovieKeywords)
            .HasForeignKey(mk => mk.KeywordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mk => mk.MovieId);
    }
}
