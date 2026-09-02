using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class TaskCommentMentionConfiguration
    : IEntityTypeConfiguration<TaskCommentMention>
{
    public void Configure(EntityTypeBuilder<TaskCommentMention> builder)
    {
        builder.ToTable("TaskCommentMentions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TaskCommentId)
            .IsRequired();

        builder.Property(x => x.MentionedUserId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasConversion(new ValueConverter<DateTime, DateTime>(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            .IsRequired();

        // Writing "@laxmi @laxmi" mentions her once, not twice.
        builder.HasIndex(x => new { x.TaskCommentId, x.MentionedUserId })
            .IsUnique();

        // "Show me everywhere I was mentioned."
        builder.HasIndex(x => x.MentionedUserId);

        builder.HasOne(x => x.TaskComment)
            .WithMany(x => x.Mentions)
            .HasForeignKey(x => x.TaskCommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MentionedUser)
            .WithMany(x => x.TaskCommentMentions)
            .HasForeignKey(x => x.MentionedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
