using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class InitiativeConfiguration : IEntityTypeConfiguration<Initiative>
{
    public void Configure(EntityTypeBuilder<Initiative> builder)
    {
        builder.ToTable("Initiatives");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .HasMaxLength(Initiative.NameMaxLength)
            .IsRequired();

        // Deliberately unbounded in SQL; the request DTO caps what may be submitted.
        builder.Property(x => x.Description)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        // Stored as the enum name, not its ordinal, so the column stays readable in SQL
        // and reordering the enum cannot silently rewrite existing rows.
        builder.Property(x => x.BusinessArea)
            .HasConversion<string>()
            .HasMaxLength(Initiative.EnumValueMaxLength)
            .IsRequired();

        builder.Property(x => x.InitiativeType)
            .HasConversion<string>()
            .HasMaxLength(Initiative.EnumValueMaxLength)
            .IsRequired();

        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(Initiative.EnumValueMaxLength)
            .IsRequired();

        builder.Property(x => x.OwnerUserId)
            .IsRequired();

        builder.Property(x => x.ExecutiveSponsorUserId);

        builder.Property(x => x.Segment)
            .HasConversion<string>()
            .HasMaxLength(Initiative.EnumValueMaxLength)
            .IsRequired();

        // One JSON column rather than a child table — same reasoning as
        // Contribution.Types/Tags/ReuseTargets: no payload of its own, and role
        // assignments never need to be queried or indexed independently of the row.
        builder.PrimitiveCollection(x => x.ImpactedRoles)
            .HasMaxLength(Initiative.ImpactedRolesMaxLength)
            .ElementType(element => element.HasConversion<string>())
            .IsRequired();

        builder.Property(x => x.ChangeImpact)
            .HasConversion<string>()
            .HasMaxLength(Initiative.EnumValueMaxLength)
            .IsRequired();

        builder.Property(x => x.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.TargetEndDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.LifecycleStage)
            .HasConversion<string>()
            .HasMaxLength(Initiative.EnumValueMaxLength)
            .IsRequired();

        builder.Property(x => x.Health)
            .HasConversion<string>()
            .HasMaxLength(Initiative.EnumValueMaxLength)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(Initiative.EnumValueMaxLength)
            .IsRequired();

        builder.Property(x => x.KeyObjective)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.ExpectedOutcome)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.SuccessMeasures)
            .HasColumnType("nvarchar(max)");

        // datetime2 stores no offset, so EF hands these back as Unspecified and they
        // serialise without a "Z" — the browser would then read UTC as local time.
        // Tagging them on the way out keeps the wire format unambiguous.
        var utcKind = new ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasConversion(utcKind)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2")
            .HasConversion(utcKind)
            .IsRequired();

        // Owner relationship

        builder.HasOne(x => x.Owner)
            .WithMany(x => x.OwnedInitiatives)
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Executive Sponsor relationship

        builder.HasOne(x => x.ExecutiveSponsor)
            .WithMany(x => x.SponsoredInitiatives)
            .HasForeignKey(x => x.ExecutiveSponsorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}