using CineSocial.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.User;

public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.ToTable("Blocks");

        builder.HasKey(b => b.Id);

        builder.HasOne(b => b.Blocker)
            .WithMany()
            .HasForeignKey(b => b.BlockerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.BlockedUser)
            .WithMany()
            .HasForeignKey(b => b.BlockedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.BlockerId, b.BlockedUserId })
            .IsUnique();

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}
