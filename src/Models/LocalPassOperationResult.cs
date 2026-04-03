namespace Models;

/// <summary>
/// Represents the result of a persisted LocalPass session operation.
/// </summary>
public sealed class LocalPassOperationResult
{
    /// <summary>
    /// Initializes a new operation result.
    /// </summary>
    /// <param name="session">Updated unlocked session.</param>
    /// <param name="statusMessage">Status message for the UI.</param>
    /// <param name="preferredSelectionId">Optional secret identifier to select after refresh.</param>
    public LocalPassOperationResult(
        SecretVaultSession session,
        string statusMessage,
        Guid? preferredSelectionId = null)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        StatusMessage = string.IsNullOrWhiteSpace(statusMessage)
            ? throw new ArgumentException("Status message is required.", nameof(statusMessage))
            : statusMessage;
        PreferredSelectionId = preferredSelectionId;
    }

    /// <summary>
    /// Gets the updated unlocked session.
    /// </summary>
    public SecretVaultSession Session { get; }

    /// <summary>
    /// Gets the status message for the UI.
    /// </summary>
    public string StatusMessage { get; }

    /// <summary>
    /// Gets the optional preferred selection identifier.
    /// </summary>
    public Guid? PreferredSelectionId { get; }
}
