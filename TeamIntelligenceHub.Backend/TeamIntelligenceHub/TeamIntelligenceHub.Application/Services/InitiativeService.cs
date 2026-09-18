using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Domain.Entities;
using TeamIntelligenceHub.Domain.Enums;

namespace TeamIntelligenceHub.Application.Services;

public class InitiativeService : IInitiativeService
{
    private readonly IInitiativeRepository _initiativeRepository;
    private readonly IContributionRepository _contributionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;

    public InitiativeService(
        IInitiativeRepository initiativeRepository,
        IContributionRepository contributionRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage)
    {
        _initiativeRepository = initiativeRepository;
        _contributionRepository = contributionRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
    }

    public async Task<InitiativeResponseDto?> GetByIdAsync(int id)
    {
        var initiative = await _initiativeRepository.GetByIdAsync(id);

        return initiative == null
            ? null
            : MapToDto(initiative);
    }

    public async Task<List<InitiativeResponseDto>> GetAllAsync()
    {
        var initiatives = await _initiativeRepository.GetAllAsync();

        return initiatives
            .Select(MapToDto)
            .ToList();
    }

    public async Task<InitiativeResponseDto> CreateAsync(
        CreateInitiativeRequestDto request)
    {
        var caller = await GetCallerAsync();

        // The annotations already cover presence and length for callers coming through
        // the controller. Re-checking here keeps the service safe to call directly and
        // catches what annotations cannot: whitespace-only text and date relationships.
        var name = RequireText(request.Name, "Initiative Name");
        var description = RequireText(request.Description, "Description");

        // [Required] on the controller path already rejects a missing value; this
        // covers the service being called directly (e.g. from tests) without one.
        var businessArea = request.BusinessArea
            ?? throw new ValidationException("Business Area is required.");
        var initiativeType = request.InitiativeType
            ?? throw new ValidationException("Initiative Type is required.");

        var startDate = request.StartDate
            ?? throw new ValidationException("Start Date is required.");

        var targetEndDate = request.TargetEndDate
            ?? throw new ValidationException("Target End Date is required.");

        if (startDate < Initiative.EarliestStartDate)
        {
            throw new ValidationException(
                $"Start Date cannot be before {Initiative.EarliestStartDate:yyyy-MM-dd}.");
        }

        if (targetEndDate < startDate)
        {
            throw new ValidationException(
                "Target End Date must be on or after Start Date.");
        }

        if (targetEndDate > startDate.AddYears(Initiative.MaxDurationYears))
        {
            throw new ValidationException(
                $"An Initiative cannot span more than {Initiative.MaxDurationYears} years.");
        }

        // The form may leave the owner blank, in which case the creator owns it.
        var ownerUserId = request.OwnerUserId ?? caller.Id;

        var owner = await _userRepository.GetByIdAsync(ownerUserId)
            ?? throw new ValidationException(
                $"Initiative Owner {ownerUserId} does not exist.");

        if (!owner.IsActive)
        {
            throw new ValidationException(
                $"{owner.DisplayName} is deactivated and cannot own an Initiative.");
        }

        if (request.ExecutiveSponsorUserId.HasValue)
        {
            if (request.ExecutiveSponsorUserId.Value == ownerUserId)
            {
                throw new ValidationException(
                    "Executive Sponsor must be someone other than the Owner.");
            }

            var sponsor = await _userRepository
                .GetByIdAsync(request.ExecutiveSponsorUserId.Value)
                ?? throw new ValidationException(
                    $"Executive Sponsor {request.ExecutiveSponsorUserId} does not exist.");

            if (!sponsor.IsActive)
            {
                throw new ValidationException(
                    $"{sponsor.DisplayName} is deactivated and cannot sponsor an Initiative.");
            }
        }

        if (await _initiativeRepository.NameExistsAsync(name))
        {
            throw new ValidationException(
                $"An Initiative named \"{name}\" already exists.");
        }

        // Model binding already rejected anything outside the enum, so an omitted value
        // is the only case left to handle.
        var status = request.Status ?? InitiativeStatus.Active;
        var priority = request.Priority ?? InitiativePriority.Medium;
        var segment = request.Segment ?? InitiativeSegment.Enterprise;
        var changeImpact = request.ChangeImpact ?? InitiativeChangeImpact.Medium;
        var lifecycleStage = request.LifecycleStage ?? InitiativeLifecycleStage.Assess;
        var health = request.Health ?? InitiativeHealth.OnTrack;

        var now = DateTime.UtcNow;

        var initiative = new Initiative
        {
            Name = name,
            Description = description,
            BusinessArea = businessArea,
            InitiativeType = initiativeType,
            Priority = priority,
            OwnerUserId = owner.Id,
            ExecutiveSponsorUserId = request.ExecutiveSponsorUserId,
            Segment = segment,
            ImpactedRoles = request.ImpactedRoles ?? new List<EnterpriseRole>(),
            ChangeImpact = changeImpact,
            StartDate = startDate,
            TargetEndDate = targetEndDate,
            LifecycleStage = lifecycleStage,
            Health = health,
            Status = status,
            KeyObjective = TrimToNull(request.KeyObjective),
            ExpectedOutcome = TrimToNull(request.ExpectedOutcome),
            SuccessMeasures = TrimToNull(request.SuccessMeasures),
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _initiativeRepository.AddAsync(initiative);

        return MapToDto(created);
    }

    /// <summary>
    /// Open to any signed-in user for now — there is no per-Initiative ownership gate on
    /// this endpoint, matching the "open access" decision for this feature. If tighter
    /// access control is wanted later, it should apply consistently across every
    /// Initiative-scoped write, not just this one.
    /// </summary>
    public async Task<InitiativeResponseDto> UpdateAsync(
        int id, UpdateInitiativeRequestDto request)
    {
        var initiative = await RequireInitiativeAsync(id);

        var name = RequireText(request.Name, "Initiative Name");
        var description = RequireText(request.Description, "Description");

        var businessArea = request.BusinessArea
            ?? throw new ValidationException("Business Area is required.");
        var initiativeType = request.InitiativeType
            ?? throw new ValidationException("Initiative Type is required.");

        var startDate = request.StartDate
            ?? throw new ValidationException("Start Date is required.");

        var targetEndDate = request.TargetEndDate
            ?? throw new ValidationException("Target End Date is required.");

        if (startDate < Initiative.EarliestStartDate)
        {
            throw new ValidationException(
                $"Start Date cannot be before {Initiative.EarliestStartDate:yyyy-MM-dd}.");
        }

        if (targetEndDate < startDate)
        {
            throw new ValidationException(
                "Target End Date must be on or after Start Date.");
        }

        if (targetEndDate > startDate.AddYears(Initiative.MaxDurationYears))
        {
            throw new ValidationException(
                $"An Initiative cannot span more than {Initiative.MaxDurationYears} years.");
        }

        var ownerUserId = request.OwnerUserId ?? initiative.OwnerUserId;

        var owner = await _userRepository.GetByIdAsync(ownerUserId)
            ?? throw new ValidationException(
                $"Initiative Owner {ownerUserId} does not exist.");

        if (!owner.IsActive)
        {
            throw new ValidationException(
                $"{owner.DisplayName} is deactivated and cannot own an Initiative.");
        }

        if (request.ExecutiveSponsorUserId.HasValue)
        {
            if (request.ExecutiveSponsorUserId.Value == ownerUserId)
            {
                throw new ValidationException(
                    "Executive Sponsor must be someone other than the Owner.");
            }

            var sponsor = await _userRepository
                .GetByIdAsync(request.ExecutiveSponsorUserId.Value)
                ?? throw new ValidationException(
                    $"Executive Sponsor {request.ExecutiveSponsorUserId} does not exist.");

            if (!sponsor.IsActive)
            {
                throw new ValidationException(
                    $"{sponsor.DisplayName} is deactivated and cannot sponsor an Initiative.");
            }
        }

        // Skipped entirely when the name did not change, so saving an otherwise-edited
        // Initiative under its own existing name never collides with itself.
        if (!string.Equals(name, initiative.Name, StringComparison.Ordinal)
            && await _initiativeRepository.NameExistsAsync(name, excludeInitiativeId: id))
        {
            throw new ValidationException(
                $"An Initiative named \"{name}\" already exists.");
        }

        // Unlike Create, an omitted value here falls back to what the Initiative already
        // has, not a fixed default — Update means "leave unchanged", not "reset".
        initiative.Name = name;
        initiative.Description = description;
        initiative.BusinessArea = businessArea;
        initiative.InitiativeType = initiativeType;
        initiative.Priority = request.Priority ?? initiative.Priority;
        initiative.OwnerUserId = owner.Id;
        initiative.ExecutiveSponsorUserId = request.ExecutiveSponsorUserId;
        initiative.Segment = request.Segment ?? initiative.Segment;
        initiative.ImpactedRoles = request.ImpactedRoles ?? initiative.ImpactedRoles;
        initiative.ChangeImpact = request.ChangeImpact ?? initiative.ChangeImpact;
        initiative.StartDate = startDate;
        initiative.TargetEndDate = targetEndDate;
        initiative.LifecycleStage = request.LifecycleStage ?? initiative.LifecycleStage;
        initiative.Health = request.Health ?? initiative.Health;
        initiative.Status = request.Status ?? initiative.Status;
        initiative.KeyObjective = TrimToNull(request.KeyObjective);
        initiative.ExpectedOutcome = TrimToNull(request.ExpectedOutcome);
        initiative.SuccessMeasures = TrimToNull(request.SuccessMeasures);
        initiative.UpdatedAt = DateTime.UtcNow;

        await _initiativeRepository.UpdateAsync(initiative);

        // Reloaded rather than mapped in place: Owner/ExecutiveSponsor may have just
        // changed, and their navigation properties still point at whoever was loaded
        // before this method ran until the entity is fetched again.
        return await ReloadAsync(id);
    }

    public async Task<InitiativeDeletionImpactDto> GetDeletionImpactAsync(int id)
    {
        await RequireInitiativeAsync(id);

        var (contributions, tasks, activities, members) =
            await _initiativeRepository.GetDeletionImpactAsync(id);

        return new InitiativeDeletionImpactDto
        {
            ContributionCount = contributions,
            TaskCount = tasks,
            ActivityCount = activities,
            TeamMemberCount = members
        };
    }

    /// <summary>
    /// Deletes the Initiative and everything under it. Contributions, InitiativeTasks,
    /// Activities, and InitiativeMembers all cascade in the database, so nothing further
    /// is needed for those.
    /// </summary>
    /// <remarks>
    /// Known gap: this does not remove the matching chunks from the Azure AI Search
    /// index. No deletion-detection policy is configured on the indexer, and
    /// IVectorSearchClient exposes no delete capability today — closing this needs the
    /// index's real key/document-id scheme, which is set up in the Azure portal and is
    /// not visible from this repository. Deleted attachments' vector chunks will remain
    /// searchable until that follow-up lands.
    /// </remarks>
    public async Task RemoveAsync(int id, CancellationToken cancellationToken = default)
    {
        var initiative = await RequireInitiativeAsync(id);

        var contributions = await _contributionRepository.GetByInitiativeIdAsync(id);

        // Blob objects do not cascade — the same reasoning ContributionService.RemoveAsync
        // applies to one Contribution's attachments, walked here across every
        // Contribution this Initiative has.
        foreach (var contribution in contributions)
        {
            foreach (var attachment in contribution.Attachments)
            {
                await _fileStorage.DeleteAsync(
                    FileStorageArea.ContributionAttachments,
                    attachment.BlobName,
                    cancellationToken);
            }
        }

        await _initiativeRepository.RemoveAsync(initiative);
    }

    public async Task<ReadinessInsightsDto> GetReadinessInsightsAsync()
    {
        var initiatives = await _initiativeRepository.GetForReadinessInsightsAsync();

        var roleCoverage = Enum.GetValues<EnterpriseRole>()
            .Select(role => new EnterpriseRoleCoverageDto
            {
                Code = role.ToString(),
                InitiativeCount = initiatives.Count(i => i.ImpactedRoles.Contains(role)),
                HighImpactCount = initiatives.Count(i =>
                    i.ImpactedRoles.Contains(role)
                    && i.ChangeImpact == InitiativeChangeImpact.High)
            })
            .ToList();

        return new ReadinessInsightsDto
        {
            ActiveInitiatives = initiatives.Count(i => i.Status == InitiativeStatus.Active),
            AtRisk = initiatives.Count(i => i.Health == InitiativeHealth.AtRisk),
            NeedsAttention = initiatives.Count(i => i.Health == InitiativeHealth.NeedsAttention),
            TotalEnterpriseRoles = roleCoverage.Count,
            EnterpriseRolesCovered = roleCoverage.Count(r => r.InitiativeCount > 0),
            RoleCoverage = roleCoverage
        };
    }

    /// <summary>
    /// Resolves the signed-in caller to their local user row, which the Owner foreign key
    /// points at. The row is created by GET /api/users/me on first sign-in.
    /// </summary>
    private async Task<User> GetCallerAsync()
    {
        var entraObjectId = _currentUserService.EntraObjectId;

        if (string.IsNullOrWhiteSpace(entraObjectId))
        {
            throw new UnauthorizedAccessException(
                "Entra Object ID was not found.");
        }

        return await _userRepository.GetByEntraObjectIdAsync(entraObjectId)
            ?? throw new ValidationException(
                "Your profile has not been created yet. Reload the app and try again.");
    }

    private async Task<Initiative> RequireInitiativeAsync(int id)
    {
        return await _initiativeRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Initiative {id} does not exist.");
    }

    private async Task<InitiativeResponseDto> ReloadAsync(int id)
    {
        return MapToDto(await RequireInitiativeAsync(id));
    }

    /// <summary>
    /// Trims and rejects blank input. [Required] accepts "   ", which would land in a
    /// NOT NULL column as an empty string.
    /// </summary>
    private static string RequireText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static InitiativeResponseDto MapToDto(Initiative initiative)
    {
        return new InitiativeResponseDto
        {
            Id = initiative.Id,
            Name = initiative.Name,
            Description = initiative.Description,
            BusinessArea = initiative.BusinessArea,
            InitiativeType = initiative.InitiativeType,
            Priority = initiative.Priority,
            OwnerUserId = initiative.OwnerUserId,
            OwnerDisplayName = initiative.Owner?.DisplayName,
            ExecutiveSponsorUserId = initiative.ExecutiveSponsorUserId,
            ExecutiveSponsorDisplayName = initiative.ExecutiveSponsor?.DisplayName,
            Segment = initiative.Segment,
            ImpactedRoles = initiative.ImpactedRoles,
            ChangeImpact = initiative.ChangeImpact,
            StartDate = initiative.StartDate,
            TargetEndDate = initiative.TargetEndDate,
            LifecycleStage = initiative.LifecycleStage,
            Health = initiative.Health,
            Status = initiative.Status,
            KeyObjective = initiative.KeyObjective,
            ExpectedOutcome = initiative.ExpectedOutcome,
            SuccessMeasures = initiative.SuccessMeasures,
            CreatedAt = initiative.CreatedAt,
            UpdatedAt = initiative.UpdatedAt
        };
    }
}
