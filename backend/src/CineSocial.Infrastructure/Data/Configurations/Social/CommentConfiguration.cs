using CineSocial.Domain.Entities.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineSocial.Infrastructure.Data.Configurations.Social;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        builder.HasKey(c => c.Id);

        // Content
        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(10000);

        // Commentable (Movie or Post)
        builder.Property(c => c.CommentableType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(c => c.CommentableId)
            .IsRequired();

        // Depth
        builder.Property(c => c.Depth)
            .IsRequired()
            .HasDefaultValue(0);

        // Timestamps
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        // Soft delete
        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasQueryFilter(c => !c.IsDeleted);

        // User relationship
        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing relationship (Parent/Replies)
        builder.HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Reactions relationship
        builder.HasMany(c => c.Reactions)
            .WithOne(r => r.Comment)
            .HasForeignKey(r => r.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => new { c.CommentableType, c.CommentableId });
        builder.HasIndex(c => c.ParentCommentId);
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.IsDeleted);
    }
}
