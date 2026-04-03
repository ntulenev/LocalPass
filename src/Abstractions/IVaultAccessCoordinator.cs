using Models;

namespace Abstractions;

/// <summary>
/// Handles first-run setup and unlock prompts before the UI starts.
/// </summary>
public interface IVaultAccessCoordinator
{
    /// <summary>
    /// Opens or creates a vault session.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the prompt flow.</param>
    /// <returns>
    /// An unlocked vault session, or <see langword="null"/> when the user aborts the prompt flow.
    /// </returns>
    Task<SecretVaultSession?> OpenAsync(CancellationToken cancellationToken);
}
