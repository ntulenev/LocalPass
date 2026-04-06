using Models;

namespace Abstractions;

/// <summary>
/// Creates application console sessions from unlocked vault sessions.
/// </summary>
public interface ILocalPassConsoleSessionFactory
{
    /// <summary>
    /// Creates a console session for the supplied unlocked vault session.
    /// </summary>
    /// <param name="session">Unlocked vault session.</param>
    /// <returns>An application session used by the console UI.</returns>
    ILocalPassConsoleSession Create(SecretVaultSession session);
}
