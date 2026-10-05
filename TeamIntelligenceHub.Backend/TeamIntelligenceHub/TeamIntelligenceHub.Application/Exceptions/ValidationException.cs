namespace TeamIntelligenceHub.Application.Exceptions;

/// <summary>
/// Raised when a request is well-formed but breaks a business rule, such as an end date
/// that precedes the start date or an owner who does not exist. Surfaces as a 400.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="message">A description of the business rule the request breaks.</param>
    public ValidationException(string message)
        : base(message)
    {
    }
}
