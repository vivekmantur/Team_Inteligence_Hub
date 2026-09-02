namespace TeamIntelligenceHub.Application.Interfaces;

public interface ICurrentUserService
{
    string? EntraObjectId { get; }

    string? Email { get; }

    string? DisplayName { get; }

    bool IsAuthenticated { get; }
}