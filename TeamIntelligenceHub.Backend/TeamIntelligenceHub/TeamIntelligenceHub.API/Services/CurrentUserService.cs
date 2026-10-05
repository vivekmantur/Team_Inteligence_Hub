// 1. Report whether the request is authenticated
// 2. Get the signed-in user's Entra object ID
// 3. Get the signed-in user's email
// 4. Get the signed-in user's display name

using System.Security.Claims;
using TeamIntelligenceHub.Application.Interfaces;

namespace TeamIntelligenceHub.API.Services;

/// <summary>
/// Reads the signed-in user off the bearer token. Claim names vary by token version and
/// claim mapping, so each value is resolved from a list of candidate names.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentUserService"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The accessor used to reach the current request's claims principal.</param>
    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the claims principal of the current request, or null outside a request.
    /// </summary>
    private ClaimsPrincipal? Principal =>
        _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public string? EntraObjectId => FirstClaim(
        "oid",
        "http://schemas.microsoft.com/identity/claims/objectidentifier");

    /// <inheritdoc />
    public string? Email => FirstClaim(
        "preferred_username",   // v2
        "upn",                  // v1
        "unique_name",          // v1
        "email",                // optional claim, guests
        ClaimTypes.Email,
        ClaimTypes.Upn);

    /// <inheritdoc />
    public string? DisplayName => FirstClaim(
        "name",
        ClaimTypes.Name,
        ClaimTypes.GivenName);

    /// <summary>
    /// Returns the first candidate claim that carries a non-empty value.
    /// </summary>
    /// <param name="claimTypes">The claim types to try, in order of preference.</param>
    /// <returns>The first non-empty claim value, or null when none is present.</returns>
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
