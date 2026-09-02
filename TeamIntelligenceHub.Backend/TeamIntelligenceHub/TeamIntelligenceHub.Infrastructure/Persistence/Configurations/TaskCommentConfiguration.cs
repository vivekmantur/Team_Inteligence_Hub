using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.ToTable("TaskComments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TaskId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        // Unbounded in SQL; the request DTO caps what may be submitted.
        builder.Property(x => x.CommentText)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        var utcKind = new ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        var nullableUtcKind = new ValueConverter<DateTime?, DateTime?>(
            value => value,
            value => value.HasValue
                ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                : value);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasConversion(utcKind)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .HasConversion(nullableUtcKind);

        // Reading a whole thread, and finding the replies under one comment.
        builder.HasIndex(x => x.TaskId);
        builder.HasIndex(x => x.ParentCommentId);

        builder.HasOne(x => x.Task)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // The author is not owned by the comment.
        builder.HasOne(x => x.User)
            .WithMany(x => x.TaskComments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // SQL Server forbids cascade on a self-referencing key — it would be a cycle.
        // Deleting a comment that has replies is therefore blocked at the database, and
        // the service removes replies first.
        builder.HasOne(x => x.ParentComment)
            .WithMany(x => x.Replies)
            .HasForeignKey(x => x.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
