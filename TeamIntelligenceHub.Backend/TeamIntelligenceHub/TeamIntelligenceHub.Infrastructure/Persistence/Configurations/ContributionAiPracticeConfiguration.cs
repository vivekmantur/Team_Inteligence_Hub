using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Configurations;

public class ContributionAiPracticeConfiguration
    : IEntityTypeConfiguration<ContributionAiPractice>
{
    public void Configure(EntityTypeBuilder<ContributionAiPractice> builder)
    {
        builder.ToTable("ContributionAiPractices");

        // Shared primary key, as with the other conditional detail tables.
        builder.HasKey(x => x.ContributionId);

        builder.Property(x => x.ContributionId)
            .ValueGeneratedNever();

        builder.Property(x => x.Tool)
            .HasMaxLength(ContributionAiPractice.ToolMaxLength)
            .IsRequired();

        builder.Property(x => x.UseCase)
            .HasMaxLength(ContributionAiPractice.UseCaseMaxLength);

        // Unbounded in SQL. A prompt is pasted verbatim so others can reuse it, and a
        // truncated prompt is worse than no prompt.
        builder.Property(x => x.Prompt)
            .HasColumnType("nvarchar(max)");

        // Numeric so it can be summed across contributions, which is the point of it.
        builder.Property(x => x.TimeSavedHoursPerWeek)
            .HasColumnType("decimal(9,2)");

        builder.Property(x => x.Recommendation)
            .HasMaxLength(ContributionAiPractice.RecommendationMaxLength);

        builder.HasOne(x => x.Contribution)
            .WithOne(x => x.AiPractice)
            .HasForeignKey<ContributionAiPractice>(x => x.ContributionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
