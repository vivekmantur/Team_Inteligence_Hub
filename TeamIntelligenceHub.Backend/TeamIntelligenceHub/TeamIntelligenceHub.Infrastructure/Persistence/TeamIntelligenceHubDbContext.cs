using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence;

/// <summary>
/// Represents the EF Core session with the Team Intelligence Hub SQL Server database and exposes one set per table.
/// </summary>
public class TeamIntelligenceHubDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeamIntelligenceHubDbContext"/> class.
    /// </summary>
    /// <param name="options">The options that configure the database provider and connection string.</param>
    public TeamIntelligenceHubDbContext(
        DbContextOptions<TeamIntelligenceHubDbContext> options)
        : base(options)
    {
    }

    /// <summary>The people who use the hub.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>The initiatives that teams work on.</summary>
    public DbSet<Initiative> Initiatives => Set<Initiative>();

    /// <summary>The people assigned to each initiative, with their roles and allocations.</summary>
    public DbSet<InitiativeMember> InitiativeMembers => Set<InitiativeMember>();

    /// <summary>The tasks raised under initiatives; maps to the "Tasks" table.</summary>
    public DbSet<InitiativeTask> Tasks => Set<InitiativeTask>();

    /// <summary>The comments and replies posted on tasks.</summary>
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    /// <summary>The users mentioned in task comments.</summary>
    public DbSet<TaskCommentMention> TaskCommentMentions => Set<TaskCommentMention>();

    /// <summary>The files attached to task comments.</summary>
    public DbSet<TaskCommentAttachment> TaskCommentAttachments =>
        Set<TaskCommentAttachment>();

    /// <summary>The posts in each initiative's activity feed.</summary>
    public DbSet<Activity> Activities => Set<Activity>();

    /// <summary>The users mentioned in activity posts.</summary>
    public DbSet<ActivityMention> ActivityMentions => Set<ActivityMention>();

    /// <summary>The contributions recorded against initiatives.</summary>
    public DbSet<Contribution> Contributions => Set<Contribution>();

    /// <summary>The people credited on each contribution.</summary>
    public DbSet<ContributionContributor> ContributionContributors =>
        Set<ContributionContributor>();

    /// <summary>The files attached to contributions.</summary>
    public DbSet<ContributionAttachment> ContributionAttachments =>
        Set<ContributionAttachment>();

    /// <summary>The links attached to contributions.</summary>
    public DbSet<ContributionLink> ContributionLinks => Set<ContributionLink>();

    /// <summary>The Business Metric details of contributions; present only for contributions typed as a Business Metric.</summary>
    public DbSet<ContributionMetric> ContributionMetrics => Set<ContributionMetric>();

    /// <summary>The Risk details of contributions; present only for contributions typed as a Risk.</summary>
    public DbSet<ContributionRisk> ContributionRisks => Set<ContributionRisk>();

    /// <summary>The AI Best Practice details of contributions; present only for contributions typed as an AI Best Practice.</summary>
    public DbSet<ContributionAiPractice> ContributionAiPractices =>
        Set<ContributionAiPractice>();

    /// <summary>The Customer Story details of contributions; present only for contributions typed as a Customer Story.</summary>
    public DbSet<ContributionCustomerStory> ContributionCustomerStories =>
        Set<ContributionCustomerStory>();

    /// <summary>The Testimonial details of contributions; present only for contributions typed as a Testimonial.</summary>
    public DbSet<ContributionTestimonial> ContributionTestimonials =>
        Set<ContributionTestimonial>();

    /// <summary>The testimonials and customer stories extracted from attachment content, not typed by hand.</summary>
    public DbSet<DocumentTestimonialAndCustomerStory> DocumentTestimonialsAndCustomerStories =>
        Set<DocumentTestimonialAndCustomerStory>();

    /// <summary>
    /// Applies every <see cref="IEntityTypeConfiguration{TEntity}"/> in this assembly to the model.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TeamIntelligenceHubDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}