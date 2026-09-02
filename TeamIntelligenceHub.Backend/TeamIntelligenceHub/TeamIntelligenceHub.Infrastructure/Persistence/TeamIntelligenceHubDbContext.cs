using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence;

public class TeamIntelligenceHubDbContext : DbContext
{
    public TeamIntelligenceHubDbContext(
        DbContextOptions<TeamIntelligenceHubDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Initiative> Initiatives => Set<Initiative>();

    public DbSet<InitiativeMember> InitiativeMembers => Set<InitiativeMember>();

    /// <summary>Maps to the "Tasks" table.</summary>
    public DbSet<InitiativeTask> Tasks => Set<InitiativeTask>();

    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    public DbSet<TaskCommentMention> TaskCommentMentions => Set<TaskCommentMention>();

    public DbSet<TaskCommentAttachment> TaskCommentAttachments =>
        Set<TaskCommentAttachment>();

    public DbSet<Activity> Activities => Set<Activity>();

    public DbSet<ActivityMention> ActivityMentions => Set<ActivityMention>();

    public DbSet<Contribution> Contributions => Set<Contribution>();

    public DbSet<ContributionContributor> ContributionContributors =>
        Set<ContributionContributor>();

    public DbSet<ContributionAttachment> ContributionAttachments =>
        Set<ContributionAttachment>();

    public DbSet<ContributionLink> ContributionLinks => Set<ContributionLink>();

    /// <summary>Present only for contributions typed as a Business Metric.</summary>
    public DbSet<ContributionMetric> ContributionMetrics => Set<ContributionMetric>();

    /// <summary>Present only for contributions typed as a Risk.</summary>
    public DbSet<ContributionRisk> ContributionRisks => Set<ContributionRisk>();

    /// <summary>Present only for contributions typed as an AI Best Practice.</summary>
    public DbSet<ContributionAiPractice> ContributionAiPractices =>
        Set<ContributionAiPractice>();

    /// <summary>Present only for contributions typed as a Customer Story.</summary>
    public DbSet<ContributionCustomerStory> ContributionCustomerStories =>
        Set<ContributionCustomerStory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TeamIntelligenceHubDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}