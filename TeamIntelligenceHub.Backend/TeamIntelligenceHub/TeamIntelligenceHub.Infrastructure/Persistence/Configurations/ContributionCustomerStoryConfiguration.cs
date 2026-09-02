using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class ContributionCustomerStoryConfiguration
    : IEntityTypeConfiguration<ContributionCustomerStory>
{
    public void Configure(EntityTypeBuilder<ContributionCustomerStory> builder)
    {
        builder.ToTable("ContributionCustomerStories");

        // Shared primary key, as with the other conditional detail tables.
        builder.HasKey(x => x.ContributionId);

        builder.Property(x => x.ContributionId)
            .ValueGeneratedNever();

        builder.Property(x => x.CustomerName)
            .HasMaxLength(ContributionCustomerStory.CustomerNameMaxLength)
            .IsRequired();

        builder.Property(x => x.Summary)
            .HasMaxLength(ContributionCustomerStory.SummaryMaxLength);

        builder.Property(x => x.Outcome)
            .HasMaxLength(ContributionCustomerStory.OutcomeMaxLength);

        builder.Property(x => x.Quote)
            .HasMaxLength(ContributionCustomerStory.QuoteMaxLength);

        builder.Property(x => x.BusinessValue)
            .HasMaxLength(ContributionCustomerStory.BusinessValueMaxLength);

        builder.HasOne(x => x.Contribution)
            .WithOne(x => x.CustomerStory)
            .HasForeignKey<ContributionCustomerStory>(x => x.ContributionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
