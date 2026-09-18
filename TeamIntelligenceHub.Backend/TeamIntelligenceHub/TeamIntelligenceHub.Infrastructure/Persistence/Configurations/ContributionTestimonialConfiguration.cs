using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class ContributionTestimonialConfiguration
    : IEntityTypeConfiguration<ContributionTestimonial>
{
    public void Configure(EntityTypeBuilder<ContributionTestimonial> builder)
    {
        builder.ToTable("ContributionTestimonials");

        // Shared primary key, as with the other conditional detail tables.
        builder.HasKey(x => x.ContributionId);

        builder.Property(x => x.ContributionId)
            .ValueGeneratedNever();

        builder.Property(x => x.Quote)
            .HasMaxLength(ContributionTestimonial.QuoteMaxLength)
            .IsRequired();

        builder.Property(x => x.SpeakerName)
            .HasMaxLength(ContributionTestimonial.SpeakerNameMaxLength)
            .IsRequired();

        builder.Property(x => x.SpeakerRole)
            .HasMaxLength(ContributionTestimonial.SpeakerRoleMaxLength);

        builder.Property(x => x.Audience)
            .HasConversion<string>()
            .HasMaxLength(ContributionTestimonial.EnumValueMaxLength)
            .IsRequired();

        builder.Property(x => x.Sentiment)
            .HasConversion<string>()
            .HasMaxLength(ContributionTestimonial.EnumValueMaxLength)
            .IsRequired();

        builder.HasOne(x => x.Contribution)
            .WithOne(x => x.Testimonial)
            .HasForeignKey<ContributionTestimonial>(x => x.ContributionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
