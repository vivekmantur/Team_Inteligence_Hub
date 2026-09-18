using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

public class ContributionRepository : IContributionRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    public ContributionRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// The full graph. Tags, Types, and ReuseTargets need no Include: they are columns
    /// on the row, which is most of the reason they are JSON rather than child tables.
    /// </summary>
    private IQueryable<Contribution> WithDetail()
    {
        return _context.Contributions
            .Include(x => x.Initiative)
            .Include(x => x.SubmittedByUser)
            .Include(x => x.Contributors)
                .ThenInclude(c => c.User)
            .Include(x => x.Links)
            .Include(x => x.Attachments)
            .Include(x => x.Metric)
            .Include(x => x.Risk)
                .ThenInclude(r => r!.OwnerUser)
            .Include(x => x.AiPractice)
            .Include(x => x.CustomerStory)
            .Include(x => x.Testimonial);
    }

    public async Task<Contribution?> GetByIdAsync(int id)
    {
        return await WithDetail().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<Contribution>> GetByInitiativeIdAsync(int initiativeId)
    {
        // Newest first: a feed is read from the top.
        return await WithDetail()
            .Where(x => x.InitiativeId == initiativeId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Contribution>> GetCustomerStoriesAsync()
    {
        return await WithDetail()
            .Where(x => x.CustomerStory != null && x.Status == ContributionStatus.Submitted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Contribution>> GetTestimonialsAsync()
    {
        return await WithDetail()
            .Where(x => x.Testimonial != null && x.Status == ContributionStatus.Submitted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<Contribution> AddAsync(Contribution contribution)
    {
        await _context.Contributions.AddAsync(contribution);

        await _context.SaveChangesAsync();

        return contribution;
    }

    public async Task UpdateAsync(Contribution contribution)
    {
        _context.Contributions.Update(contribution);

        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(Contribution contribution)
    {
        // Every child cascades in the database, including the attachment rows. The blob
        // objects behind them do not, which is why the service deletes those first.
        _context.Contributions.Remove(contribution);

        await _context.SaveChangesAsync();
    }

    public async Task ReplaceContributorsAsync(
        int contributionId,
        IReadOnlyCollection<ContributionContributor> contributors)
    {
        var existing = await _context.ContributionContributors
            .Where(x => x.ContributionId == contributionId)
            .ToListAsync();

        _context.ContributionContributors.RemoveRange(existing);

        // Saved before the insert. Without it EF can order the insert of a re-added
        // person ahead of the delete, and the unique index rejects the pair.
        await _context.SaveChangesAsync();

        if (contributors.Count > 0)
        {
            foreach (var contributor in contributors)
            {
                contributor.ContributionId = contributionId;
            }

            await _context.ContributionContributors.AddRangeAsync(contributors);

            await _context.SaveChangesAsync();
        }
    }

    public async Task ReplaceLinksAsync(
        int contributionId,
        IReadOnlyCollection<ContributionLink> links)
    {
        var existing = await _context.ContributionLinks
            .Where(x => x.ContributionId == contributionId)
            .ToListAsync();

        _context.ContributionLinks.RemoveRange(existing);

        if (links.Count > 0)
        {
            foreach (var link in links)
            {
                link.ContributionId = contributionId;
            }

            await _context.ContributionLinks.AddRangeAsync(links);
        }

        await _context.SaveChangesAsync();
    }

    public async Task SaveDetailSectionsAsync(
        int contributionId,
        ContributionMetric? metric,
        ContributionRisk? risk,
        ContributionAiPractice? aiPractice,
        ContributionCustomerStory? customerStory,
        ContributionTestimonial? testimonial)
    {
        await UpsertAsync(_context.ContributionMetrics, contributionId, metric);
        await UpsertAsync(_context.ContributionRisks, contributionId, risk);
        await UpsertAsync(_context.ContributionAiPractices, contributionId, aiPractice);
        await UpsertAsync(
            _context.ContributionCustomerStories, contributionId, customerStory);
        await UpsertAsync(
            _context.ContributionTestimonials, contributionId, testimonial);

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Writes the incoming row, replaces an existing one, or clears it when the section
    /// no longer applies. Delete and re-add rather than copy each property, because the
    /// shared primary key means the identity never changes.
    /// </summary>
    private async Task UpsertAsync<TSection>(
        DbSet<TSection> set,
        int contributionId,
        TSection? incoming)
        where TSection : class
    {
        var existing = await set.FindAsync(contributionId);

        if (existing is not null)
        {
            set.Remove(existing);

            // The re-add below reuses the same key, so the delete has to land first.
            await _context.SaveChangesAsync();
        }

        if (incoming is not null)
        {
            await set.AddAsync(incoming);
        }
    }

    public async Task<List<string>> GetTagVocabularyAsync(string? search, int take)
    {
        var query = _context.Contributions.SelectMany(x => x.Tags);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(tag => tag.Contains(term));
        }

        return await query
            .Distinct()
            .OrderBy(tag => tag)
            .Take(take)
            .ToListAsync();
    }
}
