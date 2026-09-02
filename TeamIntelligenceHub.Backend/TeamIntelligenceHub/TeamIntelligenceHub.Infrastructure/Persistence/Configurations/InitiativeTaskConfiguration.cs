using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class InitiativeTaskConfiguration
    : IEntityTypeConfiguration<InitiativeTask>
{
    public void Configure(EntityTypeBuilder<InitiativeTask> builder)
    {
        // Table name follows the schema diagram; only the C# type is renamed.
        builder.ToTable("Tasks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.InitiativeId)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(InitiativeTask.TitleMaxLength)
            .IsRequired();

        builder.Property(x => x.AssignedToUserId);

        builder.Property(x => x.CreatedByUserId)
            .IsRequired();

        builder.Property(x => x.DueDate)
            .HasColumnType("date");

        // Stored as the enum name, not its ordinal, so the column stays readable in SQL
        // and reordering the enum cannot silently rewrite existing rows.
        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(InitiativeTask.EnumValueMaxLength)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(InitiativeTask.EnumValueMaxLength)
            .IsRequired();

        // datetime2 stores no offset, so EF hands these back as Unspecified and they
        // serialise without a "Z" — the browser would then read UTC as local time.
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

        builder.Property(x => x.SourceActivityId);

        // Common reads: the board for one Initiative, and one person's workload.
        builder.HasIndex(x => x.InitiativeId);
        builder.HasIndex(x => x.AssignedToUserId);
        builder.HasIndex(x => x.SourceActivityId);

        // A task belongs to its Initiative and goes when the Initiative goes.
        builder.HasOne(x => x.Initiative)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.InitiativeId)
            .OnDelete(DeleteBehavior.Cascade);

        // People are not owned by tasks. Restrict on both, matching how Owner and
        // ExecutiveSponsor behave, and avoiding multiple cascade paths into Users.
        builder.HasOne(x => x.AssignedToUser)
            .WithMany(x => x.AssignedTasks)
            .HasForeignKey(x => x.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedTasks)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict, not SetNull: Initiatives already cascades into both Activity and
        // Tasks, so a second path into Tasks would trip SQL Server's multiple-cascade-
        // path rule. The service clears SourceActivityId before deleting a post.
        builder.HasOne(x => x.SourceActivity)
            .WithMany(x => x.CreatedTasks)
            .HasForeignKey(x => x.SourceActivityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
