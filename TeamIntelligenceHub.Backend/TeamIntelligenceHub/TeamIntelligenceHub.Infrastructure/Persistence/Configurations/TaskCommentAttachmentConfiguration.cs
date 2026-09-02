using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class TaskCommentAttachmentConfiguration
    : IEntityTypeConfiguration<TaskCommentAttachment>
{
    public void Configure(EntityTypeBuilder<TaskCommentAttachment> builder)
    {
        builder.ToTable("TaskCommentAttachments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TaskCommentId)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(TaskCommentAttachment.FileNameMaxLength)
            .IsRequired();

        builder.Property(x => x.BlobName)
            .HasMaxLength(TaskCommentAttachment.BlobNameMaxLength)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(TaskCommentAttachment.ContentTypeMaxLength)
            .IsRequired();

        builder.Property(x => x.FileSize)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasConversion(new ValueConverter<DateTime, DateTime>(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            .IsRequired();

        // One blob backs one attachment row; a duplicate key would orphan a file.
        builder.HasIndex(x => x.BlobName)
            .IsUnique();

        builder.HasIndex(x => x.TaskCommentId);

        builder.HasOne(x => x.TaskComment)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.TaskCommentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
