using Microsoft.EntityFrameworkCore;

namespace TestimonialAndCustomerStoryExtractionFunction;

/// <summary>
/// A read-only view of the main backend's ContributionAttachments row — just the two
/// columns this Function actually needs. EF only selects columns an entity declares, so
/// this intentionally maps a subset of the real table rather than the whole thing.
/// </summary>
public class ContributionAttachmentRecord
{
    public int Id { get; set; }

    public string BlobName { get; set; } = null!;
}

/// <summary>
/// Local redeclaration of the main backend's Domain entity of the same name — the write
/// side of extraction. Column names/types/lengths must match the table the main backend's
/// migration created (DocumentTestimonialsAndCustomerStories), since both sides read and
/// write the same physical table without sharing any code.
/// </summary>
public class DocumentTestimonialAndCustomerStory
{
    public const int NameMaxLength = 200;
    public const int RoleMaxLength = 200;
    public const int QuoteMaxLength = 2000;
    public const int SummaryMaxLength = 4000;
    public const int OutcomeMaxLength = 300;
    public const int BusinessValueMaxLength = 500;
    public const int EnumValueMaxLength = 50;

    public int Id { get; set; }

    public int ContributionAttachmentId { get; set; }

    public DocumentInsightType Type { get; set; }

    public string? Quote { get; set; }

    public string? CustomerName { get; set; }

    public string? Summary { get; set; }

    public string? Outcome { get; set; }

    public string? BusinessValue { get; set; }

    public string? SpeakerName { get; set; }

    public string? SpeakerRole { get; set; }

    public TestimonialAudience? Audience { get; set; }

    public TestimonialSentiment? Sentiment { get; set; }
}

/// <summary>
/// Scoped to exactly the two tables this Function touches — not the main backend's whole
/// schema. Read access to ContributionAttachments, read/write access to
/// DocumentTestimonialsAndCustomerStories.
/// </summary>
public class ExtractionDbContext : DbContext
{
    public ExtractionDbContext(DbContextOptions<ExtractionDbContext> options) : base(options)
    {
    }

    public DbSet<ContributionAttachmentRecord> ContributionAttachments => Set<ContributionAttachmentRecord>();

    public DbSet<DocumentTestimonialAndCustomerStory> DocumentTestimonialsAndCustomerStories =>
        Set<DocumentTestimonialAndCustomerStory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContributionAttachmentRecord>(builder =>
        {
            builder.ToTable("ContributionAttachments");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<DocumentTestimonialAndCustomerStory>(builder =>
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
        });

        base.OnModelCreating(modelBuilder);
    }
}
