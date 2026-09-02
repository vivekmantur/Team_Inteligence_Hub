using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class ContributionAttachmentConfiguration
    : IEntityTypeConfiguration<ContributionAttachment>
{
    public void Configure(EntityTypeBuilder<ContributionAttachment> builder)
    {
        builder.ToTable("ContributionAttachments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ContributionId)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(ContributionAttachment.FileNameMaxLength)
            .IsRequired();

        builder.Property(x => x.BlobName)
            .HasMaxLength(ContributionAttachment.BlobNameMaxLength)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(ContributionAttachment.ContentTypeMaxLength)
            .IsRequired();

        builder.Property(x => x.FileSize)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2")
            .HasConversion(new ValueConverter<DateTime, DateTime>(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            .IsRequired();

        builder.HasIndex(x => x.ContributionId);

        // The blob object is not removed by the database. Deleting it is the service's
        // job, exactly as with task comment attachments.
        builder.HasOne(x => x.Contribution)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.ContributionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
