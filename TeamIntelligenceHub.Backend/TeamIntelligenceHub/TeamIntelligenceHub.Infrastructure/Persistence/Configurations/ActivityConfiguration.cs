using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("Activity");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.InitiativeId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        // Unbounded in SQL; the request DTO caps what may be submitted.
        builder.Property(x => x.ActivityMessage)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.AutoCreateTaskEnabled)
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

        // The feed for one Initiative, newest first, is the only read that matters.
        builder.HasIndex(x => new { x.InitiativeId, x.CreatedAt });

        builder.HasOne(x => x.Initiative)
            .WithMany(x => x.Activities)
            .HasForeignKey(x => x.InitiativeId)
            .OnDelete(DeleteBehavior.Cascade);

        // The author is not owned by the post.
        builder.HasOne(x => x.User)
            .WithMany(x => x.Activities)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
