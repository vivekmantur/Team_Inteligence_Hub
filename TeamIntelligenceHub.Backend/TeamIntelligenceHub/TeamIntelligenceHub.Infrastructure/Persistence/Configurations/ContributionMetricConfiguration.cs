using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class ContributionMetricConfiguration
    : IEntityTypeConfiguration<ContributionMetric>
{
    public void Configure(EntityTypeBuilder<ContributionMetric> builder)
    {
        builder.ToTable("ContributionMetrics");

        // Shared primary key: ContributionId is both the key and the foreign key. This is
        // what makes the 1:1 real rather than a convention the service has to uphold.
        builder.HasKey(x => x.ContributionId);

        builder.Property(x => x.ContributionId)
            .ValueGeneratedNever();

        builder.Property(x => x.MetricName)
            .HasMaxLength(ContributionMetric.MetricNameMaxLength)
            .IsRequired();

        builder.Property(x => x.Unit)
            .HasMaxLength(ContributionMetric.UnitMaxLength);

        // Numeric so Analytics can compute the delta. decimal(18,4) covers counts,
        // currency, and percentages without rounding surprises.
        builder.Property(x => x.PreviousValue)
            .HasColumnType("decimal(18,4)");

        builder.Property(x => x.CurrentValue)
            .HasColumnType("decimal(18,4)");

        builder.Property(x => x.ReportingPeriod)
            .HasMaxLength(ContributionMetric.ReportingPeriodMaxLength);

        builder.HasOne(x => x.Contribution)
            .WithOne(x => x.Metric)
            .HasForeignKey<ContributionMetric>(x => x.ContributionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
