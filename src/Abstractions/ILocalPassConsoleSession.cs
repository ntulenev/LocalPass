using Models;

namespace Abstractions;

/// <summary>
/// Represents the application session consumed by the interactive console UI.
/// </summary>
public interface ILocalPassConsoleSession
{
    /// <summary>
    /// Gets the current vault snapshot.
    /// </summary>
    SecretVault CurrentVault { get; }

    /// <summary>
    /// Gets the latest status message for the UI.
    /// </summary>
    string CurrentStatusMessage { get; }

    /// <summary>
    /// Adds a new secret and persists the updated vault.
    /// </summary>
    /// <param name="input">Validated secret editor input.</param>
    /// <returns>The updated operation result.</returns>
    LocalPassOperationResult AddSecret(SecretEditorInput input);

    /// <summary>
    /// Updates an existing secret and persists the updated vault.
    /// </summary>
    /// <param name="secretId">Identifier of the secret to update.</param>
    /// <param name="input">Validated secret editor input.</param>
    /// <returns>The updated operation result.</returns>
    LocalPassOperationResult EditSecret(Guid secretId, SecretEditorInput input);

    /// <summary>
    /// Deletes an existing secret and persists the updated vault.
    /// </summary>
    /// <param name="secretId">Identifier of the secret to delete.</param>
    /// <returns>The updated operation result.</returns>
    LocalPassOperationResult DeleteSecret(Guid secretId);

    /// <summary>
    /// Adds a new secure note and persists the updated vault.
    /// </summary>
    /// <param name="input">Validated secure note editor input.</param>
    /// <returns>The updated operation result.</returns>
    LocalPassOperationResult AddNote(SecureNoteEditorInput input);

    /// <summary>
    /// Updates an existing secure note and persists the updated vault.
    /// </summary>
    /// <param name="noteId">Identifier of the secure note to update.</param>
    /// <param name="input">Validated secure note editor input.</param>
    /// <returns>The updated operation result.</returns>
    LocalPassOperationResult EditNote(Guid noteId, SecureNoteEditorInput input);

    /// <summary>
    /// Deletes an existing secure note and persists the updated vault.
    /// </summary>
    /// <param name="noteId">Identifier of the secure note to delete.</param>
    /// <returns>The updated operation result.</returns>
    LocalPassOperationResult DeleteNote(Guid noteId);

    /// <summary>
    /// Changes the master password and re-encrypts the vault.
    /// </summary>
    /// <param name="newMasterPassword">Validated new master password.</param>
    /// <returns>The updated operation result.</returns>
    LocalPassOperationResult ChangeMasterPassword(MasterPassword newMasterPassword);

    /// <summary>
    /// Opens the storage directory in the operating system shell.
    /// </summary>
    /// <returns>Status message describing the outcome.</returns>
    string OpenStorageDirectory();
}
