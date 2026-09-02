using System.Security.Claims;
using TeamIntelligenceHub.Application.Interfaces;

namespace TeamIntelligenceHub.API.Services;

/// <summary>
/// Reads the signed-in user off the bearer token.
/// </summary>
/// <remarks>
/// Claim names differ between v1 and v2 access tokens, and again depending on whether
/// inbound claim mapping is on, so each value is resolved from a list of candidates
/// rather than a single name.
/// </remarks>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal =>
        _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated ?? false;

    public string? EntraObjectId => FirstClaim(
        "oid",
        "http://schemas.microsoft.com/identity/claims/objectidentifier");

    public string? Email => FirstClaim(
        "preferred_username",   // v2
        "upn",                  // v1
        "unique_name",          // v1
        "email",                // optional claim, guests
        ClaimTypes.Email,
        ClaimTypes.Upn);

    public string? DisplayName => FirstClaim(
        "name",
        ClaimTypes.Name,
        ClaimTypes.GivenName);

    /// <summary>
    /// Returns the first candidate claim that carries a non-empty value.
    /// </summary>
    private string? FirstClaim(params string[] claimTypes)
    {
        var principal = Principal;

        if (principal is null)
        {
            return null;
        }

        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirstValue(claimType);

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
