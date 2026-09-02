using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class ContributionContributorConfiguration
    : IEntityTypeConfiguration<ContributionContributor>
{
    public void Configure(EntityTypeBuilder<ContributionContributor> builder)
    {
        builder.ToTable("ContributionContributors");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ContributionId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.ResponsibilityArea)
            .HasMaxLength(ContributionContributor.ResponsibilityAreaMaxLength);

        builder.Property(x => x.IsPrimary)
            .IsRequired();

        builder.Property(x => x.AddedAt)
            .HasColumnType("datetime2")
            .HasConversion(new ValueConverter<DateTime, DateTime>(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            .IsRequired();

        // A person is credited once per contribution. No application check survives
        // concurrent requests, so the constraint lives here.
        builder.HasIndex(x => new { x.ContributionId, x.UserId })
            .IsUnique();

        // "Contributions by person" for the Team Contributions page.
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.Contribution)
            .WithMany(x => x.Contributors)
            .HasForeignKey(x => x.ContributionId)
            .OnDelete(DeleteBehavior.Cascade);

        // A person is not owned by a contribution, so deleting one must not remove them.
        builder.HasOne(x => x.User)
            .WithMany(x => x.ContributionCredits)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
