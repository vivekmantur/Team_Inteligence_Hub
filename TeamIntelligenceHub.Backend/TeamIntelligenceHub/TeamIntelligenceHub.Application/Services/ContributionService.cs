using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.Services;

public class ContributionService : IContributionService
{
    /// <summary>
    /// Role given to somebody enrolled on an Initiative by being credited here. Matches
    /// what InitiativeTaskService uses when assigning work pulls someone onto the team.
    /// </summary>
    private const string DefaultContributorRole = "Contributor";

    private const int DefaultTagVocabularyTake = 20;
    private const int MaxTagVocabularyTake = 100;

    private readonly IContributionRepository _contributionRepository;
    private readonly IInitiativeRepository _initiativeRepository;
    private readonly IInitiativeMemberRepository _memberRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;
    private readonly IDocumentTestimonialAndCustomerStoryRepository _documentInsightRepository;

    public ContributionService(
        IContributionRepository contributionRepository,
        IInitiativeRepository initiativeRepository,
        IInitiativeMemberRepository memberRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage,
        IDocumentTestimonialAndCustomerStoryRepository documentInsightRepository)
    {
        _contributionRepository = contributionRepository;
        _initiativeRepository = initiativeRepository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
        _documentInsightRepository = documentInsightRepository;
    }

    public async Task<List<ContributionResponseDto>> GetByInitiativeAsync(int initiativeId)
    {
        await RequireInitiativeAsync(initiativeId);

        var contributions =
            await _contributionRepository.GetByInitiativeIdAsync(initiativeId);

        return contributions.Select(MapToDto).ToList();
    }

    public async Task<ContributionResponseDto> GetByIdAsync(int contributionId)
    {
        return MapToDto(await RequireContributionAsync(contributionId));
    }

    public async Task<ContributionResponseDto> CreateAsync(
        int initiativeId,
        CreateContributionRequestDto request)
    {
        await RequireInitiativeAsync(initiativeId);

        var caller = await GetCallerAsync();
        var status = request.Status ?? ContributionStatus.Draft;
        var now = DateTime.UtcNow;

        var types = NormaliseTypes(request.Types);

        RequireSectionsMatchTypes(request, types);

        var contribution = new Contribution
        {
            InitiativeId = initiativeId,
            // The submitter is whoever holds the token, never a value from the body.
            SubmittedByUserId = caller.Id,
            Title = RequireText(request.Title, "Title"),
            Description = RequireText(request.Description, "Description"),
            KeyTakeaway = Clean(request.KeyTakeaway),
            Priority = request.Priority ?? ContributionPriority.Medium,
            Status = status,
            Types = types,
            Tags = NormaliseTags(request.Tags),
            ReuseTargets = NormaliseReuseTargets(request.ReuseTargets),
            SubmittedAt = status == ContributionStatus.Submitted ? now : null,
            CreatedAt = now,
            UpdatedAt = null
        };

        var created = await _contributionRepository.AddAsync(contribution);

        await SaveGraphAsync(created.Id, initiativeId, caller.Id, request, types);

        return await ReloadAsync(created.Id);
    }

    public async Task<ContributionResponseDto> UpdateAsync(
        int contributionId,
        UpdateContributionRequestDto request)
    {
        var contribution = await RequireContributionAsync(contributionId);
        var caller = await GetCallerAsync();

        if (contribution.SubmittedByUserId != caller.Id)
        {
            throw new ValidationException(
                "You can only edit contributions you submitted.");
        }

        var types = NormaliseTypes(request.Types);

        RequireSectionsMatchTypes(request, types);

        var status = request.Status ?? contribution.Status;

        contribution.Title = RequireText(request.Title, "Title");
        contribution.Description = RequireText(request.Description, "Description");
        contribution.KeyTakeaway = Clean(request.KeyTakeaway);
        contribution.Priority = request.Priority ?? contribution.Priority;
        contribution.Types = types;
        contribution.Tags = NormaliseTags(request.Tags);
        contribution.ReuseTargets = NormaliseReuseTargets(request.ReuseTargets);
        contribution.UpdatedAt = DateTime.UtcNow;

        // Stamped once, on the way from Draft to Submitted. A later edit does not move
        // it, and nothing here can take a contribution back to Draft and unstamp it.
        if (status == ContributionStatus.Submitted
            && contribution.Status != ContributionStatus.Submitted)
        {
            contribution.Status = ContributionStatus.Submitted;
            contribution.SubmittedAt = DateTime.UtcNow;
        }

        await _contributionRepository.UpdateAsync(contribution);

        await SaveGraphAsync(
            contribution.Id, contribution.InitiativeId, caller.Id, request, types);

        return await ReloadAsync(contribution.Id);
    }

    public async Task RemoveAsync(
        int contributionId,
        CancellationToken cancellationToken = default)
    {
        var contribution = await RequireContributionAsync(contributionId);
        var caller = await GetCallerAsync();

        if (contribution.SubmittedByUserId != caller.Id)
        {
            throw new ValidationException(
                "You can only delete contributions you submitted.");
        }

        // Every child row cascades. The blob objects do not, so they go first: a stored
        // file with no row pointing at it is invisible and pays rent forever.
        foreach (var attachment in contribution.Attachments)
        {
            await _fileStorage.DeleteAsync(
                FileStorageArea.ContributionAttachments,
                attachment.BlobName,
                cancellationToken);
        }

        await _contributionRepository.RemoveAsync(contribution);
    }

    public async Task<List<CustomerStoryCardDto>> GetCustomerStoriesAsync()
    {
        var contributions = await _contributionRepository.GetCustomerStoriesAsync();

        return contributions.Select(MapToCustomerStoryCard).ToList();
    }

    public async Task<List<TestimonialCardDto>> GetTestimonialsAsync()
    {
        var contributions = await _contributionRepository.GetTestimonialsAsync();

        return contributions.Select(MapToTestimonialCard).ToList();
    }

    public async Task<List<DocumentCustomerStoryCardDto>> GetDocumentCustomerStoriesAsync()
    {
        var rows = await _documentInsightRepository.GetByTypeAsync(
            DocumentInsightType.CustomerStory);

        return rows.Select(MapToDocumentCustomerStoryCard).ToList();
    }

    public async Task<List<DocumentTestimonialCardDto>> GetDocumentTestimonialsAsync()
    {
        var rows = await _documentInsightRepository.GetByTypeAsync(
            DocumentInsightType.Testimonial);

        return rows.Select(MapToDocumentTestimonialCard).ToList();
    }

    public async Task<List<string>> GetTagVocabularyAsync(string? search, int? take)
    {
        var limit = Math.Clamp(
            take ?? DefaultTagVocabularyTake, 1, MaxTagVocabularyTake);

        return await _contributionRepository.GetTagVocabularyAsync(search, limit);
    }

    // -----------------------------------------------------------------------
    // Graph
    // -----------------------------------------------------------------------

    /// <summary>
    /// Writes the parts that live outside the root row: credited people, links, and the
    /// conditional detail sections. Shared by create and update, because the wizard
    /// submits the whole graph either way.
    /// </summary>
    private async Task SaveGraphAsync(
        int contributionId,
        int initiativeId,
        int callerId,
        CreateContributionRequestDto request,
        List<ContributionType> types)
    {
        var contributors = await BuildContributorsAsync(request.Contributors, callerId);

        await _contributionRepository.ReplaceContributorsAsync(
            contributionId, contributors);

        await _contributionRepository.ReplaceLinksAsync(
            contributionId, BuildLinks(request.Links));

        await _contributionRepository.SaveDetailSectionsAsync(
            contributionId,
            types.Contains(ContributionType.BusinessMetric)
                ? BuildMetric(contributionId, request.Metric)
                : null,
            types.Contains(ContributionType.Risk)
                ? await BuildRiskAsync(contributionId, request.Risk)
                : null,
            types.Contains(ContributionType.AiBestPractice)
                ? BuildAiPractice(contributionId, request.AiPractice)
                : null,
            types.Contains(ContributionType.CustomerStory)
                ? BuildCustomerStory(contributionId, request.CustomerStory)
                : null,
            types.Contains(ContributionType.Testimonial)
                ? BuildTestimonial(contributionId, request.Testimonial)
                : null);

        // Runs last, so a failure here cannot leave people enrolled on an Initiative for
        // a contribution that was never stored.
        foreach (var contributor in contributors)
        {
            await EnsureTeamMembershipAsync(initiativeId, contributor.UserId);
        }
    }

    /// <summary>
    /// Resolves the credited people, always including the submitter.
    /// </summary>
    /// <remarks>
    /// The submitter is added as primary when the client leaves them out, which the
    /// wizard never does but an API caller might. Duplicates are collapsed on UserId
    /// before the unique index has to reject them.
    /// </remarks>
    private async Task<List<ContributionContributor>> BuildContributorsAsync(
        List<ContributionContributorRequestDto>? requested,
        int callerId)
    {
        var now = DateTime.UtcNow;
        var byUserId = new Dictionary<int, ContributionContributor>();

        foreach (var item in requested ?? [])
        {
            var user = await _userRepository.GetByIdAsync(item.UserId)
                ?? throw new ValidationException(
                    $"Contributor {item.UserId} does not exist.");

            if (!user.IsActive)
            {
                throw new ValidationException(
                    $"{user.DisplayName} is deactivated and cannot be credited.");
            }

            // Last mention of a person wins, rather than failing the whole request.
            byUserId[user.Id] = new ContributionContributor
            {
                UserId = user.Id,
                ResponsibilityArea = Clean(item.ResponsibilityArea),
                IsPrimary = item.IsPrimary,
                AddedAt = now
            };
        }

        if (!byUserId.ContainsKey(callerId))
        {
            byUserId[callerId] = new ContributionContributor
            {
                UserId = callerId,
                ResponsibilityArea = null,
                IsPrimary = true,
                AddedAt = now
            };
        }

        return [.. byUserId.Values];
    }

    private static List<ContributionLink> BuildLinks(
        List<ContributionLinkRequestDto>? requested)
    {
        var now = DateTime.UtcNow;

        return (requested ?? [])
            .Select(item => new ContributionLink
            {
                Source = item.Source,
                Url = RequireUrl(item.Url),
                Label = Clean(item.Label),
                Description = Clean(item.Description),
                CreatedAt = now
            })
            .ToList();
    }

    private static ContributionMetric? BuildMetric(
        int contributionId,
        ContributionMetricRequestDto? request)
    {
        if (request is null)
        {
            return null;
        }

        return new ContributionMetric
        {
            ContributionId = contributionId,
            MetricName = RequireText(request.MetricName, "Metric name"),
            Unit = Clean(request.Unit),
            PreviousValue = request.PreviousValue,
            CurrentValue = request.CurrentValue,
            ReportingPeriod = Clean(request.ReportingPeriod)
        };
    }

    private async Task<ContributionRisk?> BuildRiskAsync(
        int contributionId,
        ContributionRiskRequestDto? request)
    {
        if (request is null)
        {
            return null;
        }

        if (request.OwnerUserId is int ownerId)
        {
            _ = await _userRepository.GetByIdAsync(ownerId)
                ?? throw new ValidationException(
                    $"Risk owner {ownerId} does not exist.");
        }

        return new ContributionRisk
        {
            ContributionId = contributionId,
            Description = RequireText(request.Description, "Risk description"),
            Severity = request.Severity ?? RiskSeverity.Medium,
            BusinessImpact = Clean(request.BusinessImpact),
            Mitigation = Clean(request.Mitigation),
            SupportNeeded = Clean(request.SupportNeeded),
            OwnerUserId = request.OwnerUserId,
            TargetResolutionDate = request.TargetResolutionDate
        };
    }

    private static ContributionAiPractice? BuildAiPractice(
        int contributionId,
        ContributionAiPracticeRequestDto? request)
    {
        if (request is null)
        {
            return null;
        }

        if (request.TimeSavedHoursPerWeek is decimal hours
            && (hours < 0 || hours > ContributionAiPractice.MaxTimeSavedHoursPerWeek))
        {
            throw new ValidationException(
                "Time saved must be between 0 and "
                + $"{ContributionAiPractice.MaxTimeSavedHoursPerWeek} hours per week.");
        }

        return new ContributionAiPractice
        {
            ContributionId = contributionId,
            Tool = RequireText(request.Tool, "AI tool"),
            UseCase = Clean(request.UseCase),
            Prompt = Clean(request.Prompt),
            TimeSavedHoursPerWeek = request.TimeSavedHoursPerWeek,
            Recommendation = Clean(request.Recommendation)
        };
    }

    private static ContributionCustomerStory? BuildCustomerStory(
        int contributionId,
        ContributionCustomerStoryRequestDto? request)
    {
        if (request is null)
        {
            return null;
        }

        return new ContributionCustomerStory
        {
            ContributionId = contributionId,
            CustomerName = RequireText(request.CustomerName, "Customer name"),
            Summary = Clean(request.Summary),
            Outcome = Clean(request.Outcome),
            Quote = Clean(request.Quote),
            BusinessValue = Clean(request.BusinessValue)
        };
    }

    private static ContributionTestimonial? BuildTestimonial(
        int contributionId,
        ContributionTestimonialRequestDto? request)
    {
        if (request is null)
        {
            return null;
        }

        return new ContributionTestimonial
        {
            ContributionId = contributionId,
            Quote = RequireText(request.Quote, "Quote"),
            SpeakerName = RequireText(request.SpeakerName, "Speaker name"),
            SpeakerRole = Clean(request.SpeakerRole),
            Audience = request.Audience ?? TestimonialAudience.Stakeholder,
            Sentiment = request.Sentiment ?? TestimonialSentiment.Positive
        };
    }

    /// <summary>
    /// Puts a credited person on the Initiative's team if they are not already on it.
    /// </summary>
    /// <remarks>
    /// Matches what assigning a task already does. Crediting somebody on an Initiative's
    /// work makes them part of it, and the Team tab should say so without anyone adding
    /// them by hand. The unique index on (InitiativeId, UserId) is the real guarantee
    /// against duplicates; this check only avoids attempting an insert that would
    /// violate it.
    /// </remarks>
    private async Task EnsureTeamMembershipAsync(int initiativeId, int userId)
    {
        if (await _memberRepository.ExistsAsync(initiativeId, userId))
        {
            return;
        }

        await _memberRepository.AddAsync(new InitiativeMember
        {
            InitiativeId = initiativeId,
            UserId = userId,
            Role = DefaultContributorRole,
            ResponsibilityArea = null,
            Allocation = null,
            JoinedAt = DateTime.UtcNow
        });
    }

    // -----------------------------------------------------------------------
    // Validation
    // -----------------------------------------------------------------------

    /// <summary>
    /// Refuses a detail section whose type was not selected.
    /// </summary>
    /// <remarks>
    /// This is the invariant the four separate tables exist to protect. Accepting a
    /// metric on a Progress Update would write a row nothing ever reads, because every
    /// reader keys off Types.
    /// </remarks>
    private static void RequireSectionsMatchTypes(
        CreateContributionRequestDto request,
        List<ContributionType> types)
    {
        Check(request.Metric, ContributionType.BusinessMetric, "a business metric");
        Check(request.Risk, ContributionType.Risk, "a risk");
        Check(request.AiPractice, ContributionType.AiBestPractice, "an AI best practice");
        Check(request.CustomerStory, ContributionType.CustomerStory, "a customer story");
        Check(request.Testimonial, ContributionType.Testimonial, "a testimonial");

        void Check(object? section, ContributionType required, string label)
        {
            if (section is not null && !types.Contains(required))
            {
                throw new ValidationException(
                    $"Cannot record {label} without selecting the "
                    + $"{required} contribution type.");
            }
        }
    }

    /// <summary>
    /// Collapses duplicates. A JSON column carries no unique index, so this is the only
    /// thing standing between the client and ["Risk","Risk"].
    /// </summary>
    private static List<ContributionType> NormaliseTypes(List<ContributionType>? types)
    {
        var distinct = (types ?? []).Distinct().ToList();

        if (distinct.Count < Contribution.MinTypeCount)
        {
            throw new ValidationException("Pick at least one contribution type.");
        }

        return distinct;
    }

    private static List<ContributionReuseTarget> NormaliseReuseTargets(
        List<ContributionReuseTarget>? targets)
    {
        return (targets ?? []).Distinct().ToList();
    }

    /// <summary>
    /// Trims, drops blanks, and collapses duplicates case-insensitively so "Copilot" and
    /// "copilot" cannot both survive on one contribution. The first spelling wins.
    /// </summary>
    private static List<string> NormaliseTags(List<string>? tags)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var raw in tags ?? [])
        {
            var tag = raw?.Trim();

            if (string.IsNullOrEmpty(tag))
            {
                continue;
            }

            if (tag.Length > Contribution.TagMaxLength)
            {
                throw new ValidationException(
                    $"Tag \"{tag}\" exceeds {Contribution.TagMaxLength} characters.");
            }

            if (seen.Add(tag))
            {
                result.Add(tag);
            }
        }

        return result;
    }

    private static string RequireUrl(string? url)
    {
        var value = url?.Trim();

        if (string.IsNullOrEmpty(value))
        {
            throw new ValidationException("A link URL is required.");
        }

        // Absolute http or https only. A relative or javascript: URL would be rendered
        // as a link by the client and is not evidence of anything.
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            throw new ValidationException(
                $"\"{value}\" is not a valid http or https URL.");
        }

        if (value.Length > ContributionLink.UrlMaxLength)
        {
            throw new ValidationException(
                $"Link URL cannot exceed {ContributionLink.UrlMaxLength} characters.");
        }

        return value;
    }

    private static string RequireText(string? value, string label)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ValidationException($"{label} is required.");
        }

        return trimmed;
    }

    /// <summary>Trims and turns whitespace-only into null, so blanks are not stored.</summary>
    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private async Task RequireInitiativeAsync(int initiativeId)
    {
        _ = await _initiativeRepository.GetByIdAsync(initiativeId)
            ?? throw new NotFoundException($"Initiative {initiativeId} does not exist.");
    }

    private async Task<Contribution> RequireContributionAsync(int contributionId)
    {
        return await _contributionRepository.GetByIdAsync(contributionId)
            ?? throw new NotFoundException(
                $"Contribution {contributionId} does not exist.");
    }

    private async Task<User> GetCallerAsync()
    {
        var entraObjectId = _currentUserService.EntraObjectId;

        if (string.IsNullOrWhiteSpace(entraObjectId))
        {
            throw new UnauthorizedAccessException("Entra Object ID was not found.");
        }

        return await _userRepository.GetByEntraObjectIdAsync(entraObjectId)
            ?? throw new ValidationException(
                "Your profile has not been created yet. Reload the app and try again.");
    }

    private async Task<ContributionResponseDto> ReloadAsync(int contributionId)
    {
        return MapToDto(await RequireContributionAsync(contributionId));
    }

    // -----------------------------------------------------------------------
    // Mapping
    // -----------------------------------------------------------------------

    private static ContributionResponseDto MapToDto(Contribution contribution)
    {
        return new ContributionResponseDto
        {
            Id = contribution.Id,
            InitiativeId = contribution.InitiativeId,
            // Projected, not stored. Renaming an Initiative must not leave stale copies.
            InitiativeName = contribution.Initiative?.Name ?? string.Empty,
            // BusinessArea is an enum; Workstream stays a string on this DTO so a
            // renamed/re-worded enum member never needs a Contribution-side migration.
            Workstream = contribution.Initiative?.BusinessArea.ToString() ?? string.Empty,
            SubmittedByUserId = contribution.SubmittedByUserId,
            SubmittedByDisplayName =
                contribution.SubmittedByUser?.DisplayName ?? string.Empty,
            Title = contribution.Title,
            Description = contribution.Description,
            KeyTakeaway = contribution.KeyTakeaway,
            Priority = contribution.Priority,
            Status = contribution.Status,
            Types = contribution.Types,
            Tags = contribution.Tags,
            ReuseTargets = contribution.ReuseTargets,
            SubmittedAt = contribution.SubmittedAt,
            CreatedAt = contribution.CreatedAt,
            UpdatedAt = contribution.UpdatedAt,
            Contributors = contribution.Contributors
                .OrderByDescending(c => c.IsPrimary)
                .ThenBy(c => c.AddedAt)
                .Select(c => new ContributionContributorDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    DisplayName = c.User?.DisplayName ?? string.Empty,
                    ResponsibilityArea = c.ResponsibilityArea,
                    IsPrimary = c.IsPrimary,
                    AddedAt = c.AddedAt
                })
                .ToList(),
            Links = contribution.Links
                .OrderBy(l => l.CreatedAt)
                .Select(l => new ContributionLinkDto
                {
                    Id = l.Id,
                    Source = l.Source,
                    Url = l.Url,
                    Label = l.Label,
                    Description = l.Description,
                    CreatedAt = l.CreatedAt
                })
                .ToList(),
            Attachments = contribution.Attachments
                .OrderBy(a => a.CreatedAt)
                .Select(a => new ContributionAttachmentDto
                {
                    Id = a.Id,
                    ContributionId = a.ContributionId,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    FileSize = a.FileSize,
                    CreatedAt = a.CreatedAt
                })
                .ToList(),
            Metric = contribution.Metric is null
                ? null
                : new ContributionMetricDto
                {
                    MetricName = contribution.Metric.MetricName,
                    Unit = contribution.Metric.Unit,
                    PreviousValue = contribution.Metric.PreviousValue,
                    CurrentValue = contribution.Metric.CurrentValue,
                    ReportingPeriod = contribution.Metric.ReportingPeriod
                },
            Risk = contribution.Risk is null
                ? null
                : new ContributionRiskDto
                {
                    Description = contribution.Risk.Description,
                    Severity = contribution.Risk.Severity,
                    BusinessImpact = contribution.Risk.BusinessImpact,
                    Mitigation = contribution.Risk.Mitigation,
                    SupportNeeded = contribution.Risk.SupportNeeded,
                    OwnerUserId = contribution.Risk.OwnerUserId,
                    OwnerDisplayName = contribution.Risk.OwnerUser?.DisplayName,
                    TargetResolutionDate = contribution.Risk.TargetResolutionDate
                },
            AiPractice = contribution.AiPractice is null
                ? null
                : new ContributionAiPracticeDto
                {
                    Tool = contribution.AiPractice.Tool,
                    UseCase = contribution.AiPractice.UseCase,
                    Prompt = contribution.AiPractice.Prompt,
                    TimeSavedHoursPerWeek =
                        contribution.AiPractice.TimeSavedHoursPerWeek,
                    Recommendation = contribution.AiPractice.Recommendation
                },
            CustomerStory = contribution.CustomerStory is null
                ? null
                : new ContributionCustomerStoryDto
                {
                    CustomerName = contribution.CustomerStory.CustomerName,
                    Summary = contribution.CustomerStory.Summary,
                    Outcome = contribution.CustomerStory.Outcome,
                    Quote = contribution.CustomerStory.Quote,
                    BusinessValue = contribution.CustomerStory.BusinessValue
                },
            Testimonial = contribution.Testimonial is null
                ? null
                : new ContributionTestimonialDto
                {
                    Quote = contribution.Testimonial.Quote,
                    SpeakerName = contribution.Testimonial.SpeakerName,
                    SpeakerRole = contribution.Testimonial.SpeakerRole,
                    Audience = contribution.Testimonial.Audience,
                    Sentiment = contribution.Testimonial.Sentiment
                }
        };
    }

    /// <summary>
    /// Assumes CustomerStory is not null: GetCustomerStoriesAsync already filtered on it.
    /// </summary>
    private static CustomerStoryCardDto MapToCustomerStoryCard(Contribution contribution)
    {
        var story = contribution.CustomerStory!;

        return new CustomerStoryCardDto
        {
            Id = contribution.Id,
            InitiativeId = contribution.InitiativeId,
            InitiativeName = contribution.Initiative?.Name ?? string.Empty,
            SubmittedByUserId = contribution.SubmittedByUserId,
            Title = contribution.Title,
            KeyTakeaway = contribution.KeyTakeaway,
            SubmittedAt = contribution.SubmittedAt,
            CustomerName = story.CustomerName,
            Summary = story.Summary,
            Outcome = story.Outcome,
            Quote = story.Quote,
            BusinessValue = story.BusinessValue
        };
    }

    /// <summary>
    /// Assumes Testimonial is not null: GetTestimonialsAsync already filtered on it.
    /// </summary>
    private static TestimonialCardDto MapToTestimonialCard(Contribution contribution)
    {
        var testimonial = contribution.Testimonial!;

        return new TestimonialCardDto
        {
            Id = contribution.Id,
            InitiativeId = contribution.InitiativeId,
            InitiativeName = contribution.Initiative?.Name ?? string.Empty,
            SubmittedByUserId = contribution.SubmittedByUserId,
            SubmittedAt = contribution.SubmittedAt,
            Quote = testimonial.Quote,
            SpeakerName = testimonial.SpeakerName,
            SpeakerRole = testimonial.SpeakerRole,
            Audience = testimonial.Audience,
            Sentiment = testimonial.Sentiment
        };
    }

    /// <summary>
    /// Assumes GetByTypeAsync's Include chain already loaded ContributionAttachment,
    /// its Contribution, and that Contribution's Initiative.
    /// </summary>
    private static DocumentCustomerStoryCardDto MapToDocumentCustomerStoryCard(
        DocumentTestimonialAndCustomerStory row)
    {
        var contribution = row.ContributionAttachment.Contribution;

        return new DocumentCustomerStoryCardDto
        {
            Id = row.Id,
            ContributionId = contribution.Id,
            InitiativeId = contribution.InitiativeId,
            InitiativeName = contribution.Initiative?.Name ?? string.Empty,
            SourceFileName = row.ContributionAttachment.FileName,
            CustomerName = row.CustomerName,
            Summary = row.Summary,
            Outcome = row.Outcome,
            Quote = row.Quote,
            BusinessValue = row.BusinessValue
        };
    }

    /// <summary>
    /// Assumes GetByTypeAsync's Include chain already loaded ContributionAttachment,
    /// its Contribution, and that Contribution's Initiative.
    /// </summary>
    private static DocumentTestimonialCardDto MapToDocumentTestimonialCard(
        DocumentTestimonialAndCustomerStory row)
    {
        var contribution = row.ContributionAttachment.Contribution;

        return new DocumentTestimonialCardDto
        {
            Id = row.Id,
            ContributionId = contribution.Id,
            InitiativeId = contribution.InitiativeId,
            InitiativeName = contribution.Initiative?.Name ?? string.Empty,
            SourceFileName = row.ContributionAttachment.FileName,
            Quote = row.Quote,
            SpeakerName = row.SpeakerName,
            SpeakerRole = row.SpeakerRole,
            Audience = row.Audience,
            Sentiment = row.Sentiment
        };
    }
}
