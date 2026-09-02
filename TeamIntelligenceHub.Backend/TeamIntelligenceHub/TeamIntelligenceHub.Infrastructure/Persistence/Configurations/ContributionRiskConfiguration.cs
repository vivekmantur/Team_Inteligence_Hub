using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class ContributionRiskConfiguration : IEntityTypeConfiguration<ContributionRisk>
{
    public void Configure(EntityTypeBuilder<ContributionRisk> builder)
    {
        builder.ToTable("ContributionRisks");

        // Shared primary key, as with the other conditional detail tables.
        builder.HasKey(x => x.ContributionId);

        builder.Property(x => x.ContributionId)
            .ValueGeneratedNever();

        builder.Property(x => x.Description)
            .HasMaxLength(ContributionRisk.DescriptionMaxLength)
            .IsRequired();

        builder.Property(x => x.Severity)
            .HasConversion<string>()
            .HasMaxLength(ContributionRisk.EnumValueMaxLength)
            .IsRequired();

        builder.Property(x => x.BusinessImpact)
            .HasMaxLength(ContributionRisk.BusinessImpactMaxLength);

        builder.Property(x => x.Mitigation)
            .HasMaxLength(ContributionRisk.MitigationMaxLength);

        builder.Property(x => x.SupportNeeded)
            .HasMaxLength(ContributionRisk.SupportNeededMaxLength);

        builder.Property(x => x.TargetResolutionDate)
            .HasColumnType("date");

        // "Critical risks across every Initiative" is the query this exists for.
        builder.HasIndex(x => x.Severity);

        builder.HasOne(x => x.Contribution)
            .WithOne(x => x.Risk)
            .HasForeignKey<ContributionRisk>(x => x.ContributionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict rather than SetNull: SQL Server allows only one cascade-or-set-null
        // path into this table, and the Contribution cascade above has it.
        builder.HasOne(x => x.OwnerUser)
            .WithMany(x => x.OwnedContributionRisks)
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
