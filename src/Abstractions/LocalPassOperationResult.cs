using Models;

namespace Abstractions;

/// <summary>
/// Represents the result of a persisted LocalPass application operation.
/// </summary>
public sealed class LocalPassOperationResult
{
    /// <summary>
    /// Initializes a new operation result.
    /// </summary>
    /// <param name="vault">Updated vault snapshot.</param>
    /// <param name="statusMessage">Status message for the UI.</param>
    /// <param name="preferredSelectionId">Optional secret identifier to select after refresh.</param>
    public LocalPassOperationResult(
        SecretVault vault,
        string statusMessage,
        Guid? preferredSelectionId = null)
    {
        CurrentState = new LocalPassViewState(vault, statusMessage);
        PreferredSelectionId = preferredSelectionId;
    }

    /// <summary>
    /// Gets the current view state after the operation.
    /// </summary>
    public LocalPassViewState CurrentState { get; }

    /// <summary>
    /// Gets the optional preferred selection identifier.
    /// </summary>
    public Guid? PreferredSelectionId { get; }
}
