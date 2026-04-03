using Abstractions;

namespace Infrastructure;

/// <summary>
/// Provides the current UTC time from the local system clock.
/// </summary>
public sealed class DefaultClock : IClock
{
    /// <summary>
    /// Gets the current UTC timestamp.
    /// </summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
