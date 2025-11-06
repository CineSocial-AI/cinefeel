using CineSocial.Domain.Entities.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Social;

public class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("Reactions");

        builder.HasKey(r => r.Id);

        // Reaction type
        builder.Property(r => r.Type)
            .IsRequired()
            .HasConversion<int>();

        // Timestamp
        builder.Property(r => r.CreatedAt)
            .IsRequired();

        // User relationship
        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comment relationship (configured in CommentConfiguration)

        // Unique constraint: One user can only have one reaction per comment
        builder.HasIndex(r => new { r.UserId, r.CommentId })
            .IsUnique();

        // Index for frequent queries
        builder.HasIndex(r => r.CommentId);
    }
}
