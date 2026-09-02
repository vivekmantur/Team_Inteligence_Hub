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
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public InitiativeService(
        IInitiativeRepository initiativeRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _initiativeRepository = initiativeRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
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
