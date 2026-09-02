namespace TeamIntelligenceHub.Domain.Enums;

/// <summary>
/// An enterprise role an Initiative's change can land on. Codes match how the
/// business already refers to these roles (AE, ATS, etc.), not full titles.
/// </summary>
public enum EnterpriseRole
{
    AE,
    ATS,
    SSP,
    SE,
    CE,
    CSA,
    CSAM
}
