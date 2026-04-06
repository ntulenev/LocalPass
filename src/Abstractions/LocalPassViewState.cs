using Models;

namespace Abstractions;

/// <summary>
/// Represents the UI state exposed by the LocalPass application layer.
/// </summary>
public sealed class LocalPassViewState
{
    /// <summary>
    /// Initializes a new view state snapshot.
    /// </summary>
    /// <param name="vault">Current vault snapshot.</param>
    /// <param name="statusMessage">Status message for the UI.</param>
    public LocalPassViewState(SecretVault vault, string statusMessage)
    {
        Vault = vault ?? throw new ArgumentNullException(nameof(vault));
        StatusMessage = string.IsNullOrWhiteSpace(statusMessage)
            ? throw new ArgumentException("Status message is required.", nameof(statusMessage))
            : statusMessage;
    }

    /// <summary>
    /// Gets the current vault snapshot.
    /// </summary>
    public SecretVault Vault { get; }

    /// <summary>
    /// Gets the current status message for the UI.
    /// </summary>
    public string StatusMessage { get; }
}
