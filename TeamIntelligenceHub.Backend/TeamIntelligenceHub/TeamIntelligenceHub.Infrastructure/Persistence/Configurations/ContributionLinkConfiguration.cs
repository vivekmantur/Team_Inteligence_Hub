using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class ContributionLinkConfiguration : IEntityTypeConfiguration<ContributionLink>
{
    public void Configure(EntityTypeBuilder<ContributionLink> builder)
    {
        builder.ToTable("ContributionLinks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ContributionId)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasConversion<string>()
            .HasMaxLength(ContributionLink.EnumValueMaxLength)
            .IsRequired();

        builder.Property(x => x.Url)
            .HasMaxLength(ContributionLink.UrlMaxLength)
            .IsRequired();

        builder.Property(x => x.Label)
            .HasMaxLength(ContributionLink.LabelMaxLength);

        builder.Property(x => x.Description)
            .HasMaxLength(ContributionLink.DescriptionMaxLength);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasConversion(new ValueConverter<DateTime, DateTime>(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            .IsRequired();

        builder.HasIndex(x => x.ContributionId);

        builder.HasOne(x => x.Contribution)
            .WithMany(x => x.Links)
            .HasForeignKey(x => x.ContributionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
