namespace Abstractions;

/// <summary>
/// Renders the interactive secret vault UI.
/// </summary>
public interface ISecretVaultConsoleRenderer
{
    /// <summary>
    /// Starts the interactive secret vault session.
    /// </summary>
    /// <param name="session">Interactive console session.</param>
    /// <param name="cancellationToken">Token used to stop the UI loop.</param>
    /// <returns>A task that completes when the UI exits.</returns>
    Task RunAsync(ILocalPassConsoleSession session, CancellationToken cancellationToken);
}
