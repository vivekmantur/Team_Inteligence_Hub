using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class ContributionConfiguration : IEntityTypeConfiguration<Contribution>
{
    public void Configure(EntityTypeBuilder<Contribution> builder)
    {
        builder.ToTable("Contributions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.InitiativeId)
            .IsRequired();

        builder.Property(x => x.SubmittedByUserId)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(Contribution.TitleMaxLength)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(Contribution.DescriptionMaxLength)
            .IsRequired();

        builder.Property(x => x.KeyTakeaway)
            .HasMaxLength(Contribution.KeyTakeawayMaxLength);

        // Enums store their member name, so a row stays readable in SSMS and reordering
        // the enum cannot silently rewrite existing rows.
        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(Contribution.EnumValueMaxLength)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(Contribution.EnumValueMaxLength)
            .IsRequired();

        // Three lists with no payload of their own. EF Core 9 primitive collections map
        // each to one JSON column on this row rather than a child table. HasMaxLength
        // matters: without it EF emits nvarchar(max), which is stored off-row.
        //
        // Uniqueness within a list cannot be enforced here the way a unique index did on
        // a child table, so the service deduplicates before saving.
        builder.PrimitiveCollection(x => x.Types)
            .HasMaxLength(Contribution.TypesMaxLength)
            .ElementType(element => element.HasConversion<string>())
            .IsRequired();

        builder.PrimitiveCollection(x => x.Tags)
            .HasMaxLength(Contribution.TagsMaxLength)
            .ElementType(element => element.HasMaxLength(Contribution.TagMaxLength))
            .IsRequired();

        builder.PrimitiveCollection(x => x.ReuseTargets)
            .HasMaxLength(Contribution.ReuseTargetsMaxLength)
            .ElementType(element => element.HasConversion<string>())
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

        builder.Property(x => x.SubmittedAt)
            .HasColumnType("datetime2")
            .HasConversion(nullableUtcKind);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasConversion(utcKind)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .HasConversion(nullableUtcKind);

        // The feed for one Initiative, newest first.
        builder.HasIndex(x => new { x.InitiativeId, x.CreatedAt });

        // "My contributions" across every Initiative.
        builder.HasIndex(x => x.SubmittedByUserId);

        // A contribution belongs to its Initiative and goes when the Initiative goes.
        builder.HasOne(x => x.Initiative)
            .WithMany(x => x.Contributions)
            .HasForeignKey(x => x.InitiativeId)
            .OnDelete(DeleteBehavior.Cascade);

        // The submitter is not owned by the contribution. Restrict also keeps this off
        // the single cascade path SQL Server allows into this table.
        builder.HasOne(x => x.SubmittedByUser)
            .WithMany(x => x.SubmittedContributions)
            .HasForeignKey(x => x.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
