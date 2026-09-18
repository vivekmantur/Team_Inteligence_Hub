using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class DocumentTestimonialAndCustomerStoryConfiguration
    : IEntityTypeConfiguration<DocumentTestimonialAndCustomerStory>
{
    public void Configure(EntityTypeBuilder<DocumentTestimonialAndCustomerStory> builder)
    {
        builder.ToTable("DocumentTestimonialsAndCustomerStories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(DocumentTestimonialAndCustomerStory.EnumValueMaxLength)
            .IsRequired();

        builder.Property(x => x.Quote)
            .HasMaxLength(DocumentTestimonialAndCustomerStory.QuoteMaxLength);

        builder.Property(x => x.CustomerName)
            .HasMaxLength(DocumentTestimonialAndCustomerStory.NameMaxLength);

        builder.Property(x => x.Summary)
            .HasMaxLength(DocumentTestimonialAndCustomerStory.SummaryMaxLength);

        builder.Property(x => x.Outcome)
            .HasMaxLength(DocumentTestimonialAndCustomerStory.OutcomeMaxLength);

        builder.Property(x => x.BusinessValue)
            .HasMaxLength(DocumentTestimonialAndCustomerStory.BusinessValueMaxLength);

        builder.Property(x => x.SpeakerName)
            .HasMaxLength(DocumentTestimonialAndCustomerStory.NameMaxLength);

        builder.Property(x => x.SpeakerRole)
            .HasMaxLength(DocumentTestimonialAndCustomerStory.RoleMaxLength);

        builder.Property(x => x.Audience)
            .HasConversion<string>()
            .HasMaxLength(DocumentTestimonialAndCustomerStory.EnumValueMaxLength);

        builder.Property(x => x.Sentiment)
            .HasConversion<string>()
            .HasMaxLength(DocumentTestimonialAndCustomerStory.EnumValueMaxLength);

        // Cascade: if the source attachment is deleted, whatever was extracted from it
        // has no reason to exist either.
        builder.HasOne(x => x.ContributionAttachment)
            .WithMany()
            .HasForeignKey(x => x.ContributionAttachmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ContributionAttachmentId);
    }
}
