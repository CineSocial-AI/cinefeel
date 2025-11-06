using CineSocial.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.User;

public class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    public void Configure(EntityTypeBuilder<Image> builder)
    {
        builder.ToTable("Images");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(i => i.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.Data)
            .IsRequired();

        builder.Property(i => i.Size)
            .IsRequired();

        // Query Filter for soft delete
        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
