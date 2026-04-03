namespace Abstractions;

/// <summary>
/// Provides the current UTC time.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC timestamp.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
