namespace TeamIntelligenceHub.Application.Exceptions;

/// <summary>
/// Raised when a requested record does not exist, or exists but does not belong to the
/// parent named in the route. Surfaces as a 404.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="message">A description of the record that was not found.</param>
    public NotFoundException(string message)
        : base(message)
    {
    }
}
