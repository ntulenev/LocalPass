using Models;

namespace Abstractions;

/// <summary>
/// Performs session-level LocalPass operations outside of the UI layer.
/// </summary>
public interface ILocalPassSessionOperations
{
    /// <summary>
    /// Adds a new secret to the current session and persists it.
    /// </summary>
    /// <param name="session">Unlocked session.</param>
    /// <param name="input">Input used to create the secret.</param>
    /// <returns>The operation result.</returns>
    LocalPassOperationResult AddSecret(SecretVaultSession session, SecretEditorInput input);

    /// <summary>
    /// Updates an existing secret and persists it.
    /// </summary>
    /// <param name="session">Unlocked session.</param>
    /// <param name="secret">Secret to update.</param>
    /// <param name="input">Updated secret input.</param>
    /// <returns>The operation result.</returns>
    LocalPassOperationResult EditSecret(
        SecretVaultSession session,
        SecretRecord secret,
        SecretEditorInput input);

    /// <summary>
    /// Deletes an existing secret and persists the session.
    /// </summary>
    /// <param name="session">Unlocked session.</param>
    /// <param name="secret">Secret to delete.</param>
    /// <returns>The operation result.</returns>
    LocalPassOperationResult DeleteSecret(SecretVaultSession session, SecretRecord secret);

    /// <summary>
    /// Changes the master password and persists the session.
    /// </summary>
    /// <param name="session">Unlocked session.</param>
    /// <param name="newMasterPassword">New master password.</param>
    /// <returns>The operation result.</returns>
    LocalPassOperationResult ChangeMasterPassword(
        SecretVaultSession session,
        MasterPassword newMasterPassword);

    /// <summary>
    /// Opens the LocalPass storage directory.
    /// </summary>
    /// <returns>Status message describing the outcome.</returns>
    string OpenStorageDirectory();
}
