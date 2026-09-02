using TeamIntelligenceHub.Application.DTOs;
using TeamIntelligenceHub.Application.Exceptions;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Application.Interfaces.Services;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Application.Services;

public class InitiativeMemberService : IInitiativeMemberService
{
    /// <summary>
    /// The UI's catch-all option. It is a prompt to type a real role, never a role
    /// itself, so it must not reach the column.
    /// </summary>
    private const string PlaceholderRole = "Other";

    private readonly IInitiativeMemberRepository _memberRepository;
    private readonly IInitiativeRepository _initiativeRepository;
    private readonly IUserRepository _userRepository;

    public InitiativeMemberService(
        IInitiativeMemberRepository memberRepository,
        IInitiativeRepository initiativeRepository,
        IUserRepository userRepository)
    {
        _memberRepository = memberRepository;
        _initiativeRepository = initiativeRepository;
        _userRepository = userRepository;
    }

    public async Task<List<InitiativeMemberResponseDto>> GetByInitiativeAsync(
        int initiativeId)
    {
        await RequireInitiativeAsync(initiativeId);

        var members = await _memberRepository
            .GetByInitiativeIdAsync(initiativeId);

        return members
            .Select(MapToDto)
            .ToList();
    }

    public async Task<InitiativeMemberResponseDto> AddAsync(
        int initiativeId,
        AddInitiativeMemberRequestDto request)
    {
        await RequireInitiativeAsync(initiativeId);

        var userId = request.UserId
            ?? throw new ValidationException("A team member is required.");

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new ValidationException($"User {userId} does not exist.");

        if (!user.IsActive)
        {
            throw new ValidationException(
                $"{user.DisplayName} is deactivated and cannot be added to a team.");
        }

        // The unique index is the real guarantee; this exists to give a readable
        // message instead of a constraint violation.
        if (await _memberRepository.ExistsAsync(initiativeId, userId))
        {
            throw new ValidationException(
                $"{user.DisplayName} is already on this Initiative's team.");
        }

        var member = new InitiativeMember
        {
            InitiativeId = initiativeId,
            UserId = userId,
            Role = RequireRole(request.Role),
            ResponsibilityArea = TrimToNull(request.ResponsibilityArea),
            Allocation = RequireAllocation(request.Allocation),
            JoinedAt = DateTime.UtcNow
        };

        var created = await _memberRepository.AddAsync(member);

        return MapToDto(created);
    }

    public async Task<InitiativeMemberResponseDto> UpdateAsync(
        int initiativeId,
        int memberId,
        UpdateInitiativeMemberRequestDto request)
    {
        var member = await RequireMemberAsync(initiativeId, memberId);

        member.Role = RequireRole(request.Role);
        member.ResponsibilityArea = TrimToNull(request.ResponsibilityArea);
        member.Allocation = RequireAllocation(request.Allocation);

        await _memberRepository.UpdateAsync(member);

        return MapToDto(member);
    }

    public async Task RemoveAsync(int initiativeId, int memberId)
    {
        var member = await RequireMemberAsync(initiativeId, memberId);

        await _memberRepository.RemoveAsync(member);
    }

    private async Task RequireInitiativeAsync(int initiativeId)
    {
        _ = await _initiativeRepository.GetByIdAsync(initiativeId)
            ?? throw new NotFoundException(
                $"Initiative {initiativeId} does not exist.");
    }

    /// <summary>
    /// Loads a membership and confirms it belongs to the Initiative in the route, so a
    /// member of one Initiative cannot be edited or removed through another's URL.
    /// </summary>
    private async Task<InitiativeMember> RequireMemberAsync(
        int initiativeId,
        int memberId)
    {
        var member = await _memberRepository.GetByIdAsync(memberId)
            ?? throw new NotFoundException(
                $"Team member {memberId} does not exist.");

        if (member.InitiativeId != initiativeId)
        {
            throw new NotFoundException(
                $"Team member {memberId} does not belong to Initiative {initiativeId}.");
        }

        return member;
    }

    /// <summary>
    /// Rejects blank input and the UI's "Other" placeholder, which means the person
    /// chose Other and left the "Specify role" box empty.
    /// </summary>
    private static string RequireRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ValidationException("Role is required.");
        }

        var trimmed = role.Trim();

        if (string.Equals(trimmed, PlaceholderRole, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                "Specify the role when choosing Other.");
        }

        return trimmed;
    }

    private static decimal? RequireAllocation(decimal? allocation)
    {
        if (allocation is null)
        {
            return null;
        }

        if (allocation < InitiativeMember.MinAllocationPercent ||
            allocation > InitiativeMember.MaxAllocationPercent)
        {
            throw new ValidationException(
                $"Allocation must be between {InitiativeMember.MinAllocationPercent} " +
                $"and {InitiativeMember.MaxAllocationPercent} percent.");
        }

        // decimal(5,2) keeps two places; round rather than let SQL truncate silently.
        return decimal.Round(allocation.Value, 2);
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static InitiativeMemberResponseDto MapToDto(InitiativeMember member)
    {
        return new InitiativeMemberResponseDto
        {
            Id = member.Id,
            InitiativeId = member.InitiativeId,
            UserId = member.UserId,
            UserDisplayName = member.User?.DisplayName ?? string.Empty,
            UserEmail = member.User?.Email ?? string.Empty,
            Role = member.Role,
            ResponsibilityArea = member.ResponsibilityArea,
            Allocation = member.Allocation,
            JoinedAt = member.JoinedAt
        };
    }
}
