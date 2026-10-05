namespace TeamIntelligenceHub.Application.Exceptions;

/// <summary>
/// Raised when the file store itself fails — bad credentials, missing container,
/// network trouble. Wraps the provider's exception so the API layer needs no storage
/// SDK dependency.
/// </summary>
public class FileStorageException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileStorageException"/> class.
    /// </summary>
    /// <param name="message">The reason the file store operation failed.</param>
    /// <param name="innerException">The storage SDK exception that caused the failure, if any.</param>
    public FileStorageException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
