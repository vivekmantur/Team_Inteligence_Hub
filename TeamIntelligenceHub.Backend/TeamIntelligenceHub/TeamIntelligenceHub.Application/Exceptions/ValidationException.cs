namespace TeamIntelligenceHub.Application.Exceptions;

/// <summary>
/// Raised when a request is well-formed but breaks a business rule, such as an end date
/// that precedes the start date or an owner who does not exist. Surfaces as a 400.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message)
        : base(message)
    {
    }
}
