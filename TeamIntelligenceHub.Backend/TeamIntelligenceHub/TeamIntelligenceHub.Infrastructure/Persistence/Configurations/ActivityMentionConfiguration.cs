using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class ActivityMentionConfiguration : IEntityTypeConfiguration<ActivityMention>
{
    public void Configure(EntityTypeBuilder<ActivityMention> builder)
    {
        builder.ToTable("ActivityMentions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ActivityId)
            .IsRequired();

        builder.Property(x => x.MentionedUserId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasConversion(new ValueConverter<DateTime, DateTime>(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            .IsRequired();

        // Writing "@laxmi @laxmi" mentions her once, and auto-create raises one task
        // rather than two. Nothing above the database can guarantee that concurrently.
        builder.HasIndex(x => new { x.ActivityId, x.MentionedUserId })
            .IsUnique();

        // "Show me everywhere I was mentioned."
        builder.HasIndex(x => x.MentionedUserId);

        builder.HasOne(x => x.Activity)
            .WithMany(x => x.Mentions)
            .HasForeignKey(x => x.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MentionedUser)
            .WithMany(x => x.ActivityMentions)
            .HasForeignKey(x => x.MentionedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
