// 1. Expose the ContributionAttachments table for reads
// 2. Expose the DocumentTestimonialsAndCustomerStories table for reads and writes
// 3. Map both entities to their existing tables and column limits

using Microsoft.EntityFrameworkCore;

namespace TestimonialAndCustomerStoryExtractionFunction;

/// <summary>
/// A read-only view of the main backend's ContributionAttachments row — just the two
/// columns this Function actually needs. EF only selects columns an entity declares, so
/// this intentionally maps a subset of the real table rather than the whole thing.
/// </summary>
public class ContributionAttachmentRecord
{
    /// <summary>Gets or sets the attachment ID.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the name of the attachment's blob in storage.</summary>
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
    /// <summary>The maximum length of customer and speaker names.</summary>
    public const int NameMaxLength = 200;
    /// <summary>The maximum length of the speaker role.</summary>
    public const int RoleMaxLength = 200;
    /// <summary>The maximum length of the quote.</summary>
    public const int QuoteMaxLength = 2000;
    /// <summary>The maximum length of the summary.</summary>
    public const int SummaryMaxLength = 4000;
    /// <summary>The maximum length of the outcome.</summary>
    public const int OutcomeMaxLength = 300;
    /// <summary>The maximum length of the business value.</summary>
    public const int BusinessValueMaxLength = 500;
    /// <summary>The maximum length of enum values stored as strings.</summary>
    public const int EnumValueMaxLength = 50;

    /// <summary>Gets or sets the row ID.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the ID of the attachment the insight came from.</summary>
    public int ContributionAttachmentId { get; set; }

    /// <summary>Gets or sets whether the row is a customer story or a testimonial.</summary>
    public DocumentInsightType Type { get; set; }

    /// <summary>Gets or sets the extracted quote.</summary>
    public string? Quote { get; set; }

    /// <summary>Gets or sets the external customer's name, for a customer story.</summary>
    public string? CustomerName { get; set; }

    /// <summary>Gets or sets the customer story summary.</summary>
    public string? Summary { get; set; }

    /// <summary>Gets or sets the headline result of a customer story.</summary>
    public string? Outcome { get; set; }

    /// <summary>Gets or sets the business value or impact of a customer story.</summary>
    public string? BusinessValue { get; set; }

    /// <summary>Gets or sets the testimonial speaker's name.</summary>
    public string? SpeakerName { get; set; }

    /// <summary>Gets or sets the testimonial speaker's role or title.</summary>
    public string? SpeakerRole { get; set; }

    /// <summary>Gets or sets the testimonial's audience.</summary>
    public TestimonialAudience? Audience { get; set; }

    /// <summary>Gets or sets the testimonial's sentiment.</summary>
    public TestimonialSentiment? Sentiment { get; set; }
}

/// <summary>
/// Scoped to exactly the two tables this Function touches — not the main backend's whole
/// schema. Read access to ContributionAttachments, read/write access to
/// DocumentTestimonialsAndCustomerStories.
/// </summary>
public class ExtractionDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractionDbContext"/> class.
    /// </summary>
    /// <param name="options">The options that configure the database connection.</param>
    public ExtractionDbContext(DbContextOptions<ExtractionDbContext> options) : base(options)
    {
    }

    /// <summary>Gets the contribution attachments, used read-only.</summary>
    public DbSet<ContributionAttachmentRecord> ContributionAttachments => Set<ContributionAttachmentRecord>();

    /// <summary>Gets the extracted testimonials and customer stories.</summary>
    public DbSet<DocumentTestimonialAndCustomerStory> DocumentTestimonialsAndCustomerStories =>
        Set<DocumentTestimonialAndCustomerStory>();

    /// <summary>
    /// Maps both entities to their existing tables, stores enums as strings and applies the column length limits.
    /// </summary>
    /// <param name="modelBuilder">The builder used to configure the model.</param>
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
