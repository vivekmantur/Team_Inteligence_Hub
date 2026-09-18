namespace TeamIntelligenceHub.Application.DTOs;

/// <summary>
/// Feeds the Insights page's Readiness and Audience &amp; Roles tabs from one call: both
/// are aggregate reads over every Initiative, so a client showing them together (the
/// Overview tab does) would otherwise need two round trips for the same underlying data.
/// </summary>
public class ReadinessInsightsDto
{
    public int ActiveInitiatives { get; set; }

    public int AtRisk { get; set; }

    public int NeedsAttention { get; set; }

    /// <summary>How many EnterpriseRole members exist. RoleCoverage always has this many rows.</summary>
    public int TotalEnterpriseRoles { get; set; }

    /// <summary>Roles named in at least one Initiative's ImpactedRoles.</summary>
    public int EnterpriseRolesCovered { get; set; }

    public List<EnterpriseRoleCoverageDto> RoleCoverage { get; set; } = [];
}

public class EnterpriseRoleCoverageDto
{
    /// <summary>The EnterpriseRole member name (e.g. "AE"). The client owns the display label.</summary>
    public string Code { get; set; } = null!;

    public int InitiativeCount { get; set; }

    public int HighImpactCount { get; set; }
}
