namespace TeamIntelligenceHub.Application.Exceptions;

/// <summary>
/// Raised when the file store itself fails — bad credentials, missing container,
/// network trouble.
/// </summary>
/// <remarks>
/// Wraps the provider's own exception type so controllers can report a useful reason
/// without the API layer taking a dependency on the storage SDK.
/// </remarks>
public class FileStorageException : Exception
{
    public FileStorageException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
