namespace TeamIntelligenceHub.Application.Exceptions;

/// <summary>
/// Raised when a requested record does not exist, or exists but does not belong to the
/// parent named in the route. Surfaces as a 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}
