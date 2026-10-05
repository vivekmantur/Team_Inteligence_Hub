namespace TeamIntelligenceHub.Application.Interfaces;

/// <summary>
/// Exposes the identity of the signed-in caller, read from the bearer token.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>The caller's Entra object id, or null when the token carries none.</summary>
    string? EntraObjectId { get; }

    /// <summary>The caller's email claim, or null when the token carries none.</summary>
    string? Email { get; }

    /// <summary>The caller's display name claim, or null when the token carries none.</summary>
    string? DisplayName { get; }

    /// <summary>True when the request carries an authenticated identity.</summary>
    bool IsAuthenticated { get; }
}
