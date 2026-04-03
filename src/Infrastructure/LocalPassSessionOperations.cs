using Abstractions;

using Models;

namespace Infrastructure;

/// <summary>
/// Performs LocalPass session operations outside of the UI layer.
/// </summary>
public sealed class LocalPassSessionOperations : ILocalPassSessionOperations
{
    /// <summary>
    /// Initializes a new operations service.
    /// </summary>
    /// <param name="vaultStore">Encrypted vault store.</param>
    /// <param name="clock">Clock used for new timestamps.</param>
    /// <param name="storageLocation">Provider for the LocalPass storage directory path.</param>
    /// <param name="folderOpener">Adapter used to open directories in the OS shell.</param>
    public LocalPassSessionOperations(
        ISecretVaultStore vaultStore,
        IClock clock,
        ISecretVaultStorageLocation storageLocation,
        IFolderOpener folderOpener)
    {
        _vaultStore = vaultStore ?? throw new ArgumentNullException(nameof(vaultStore));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _storageLocation = storageLocation ?? throw new ArgumentNullException(nameof(storageLocation));
        _folderOpener = folderOpener ?? throw new ArgumentNullException(nameof(folderOpener));
    }

    /// <inheritdoc />
    public LocalPassOperationResult AddSecret(SecretVaultSession session, SecretEditorInput input)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(input);

        var timestamp = _clock.UtcNow;
        var record = input.ToRecord(timestamp);
        var updatedVault = session.Vault.WithEntry(record, timestamp);
        var updatedSession = _vaultStore.Save(session.WithVault(updatedVault));
        return new LocalPassOperationResult(updatedSession, $"Saved {record.Source.Value}.", record.Id);
    }

    /// <inheritdoc />
    public LocalPassOperationResult EditSecret(
        SecretVaultSession session,
        SecretRecord secret,
        SecretEditorInput input)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentNullException.ThrowIfNull(input);

        var timestamp = _clock.UtcNow;
        var updatedRecord = input.ApplyTo(secret, timestamp);
        var updatedVault = session.Vault.WithEntry(updatedRecord, timestamp);
        var updatedSession = _vaultStore.Save(session.WithVault(updatedVault));
        return new LocalPassOperationResult(
            updatedSession,
            $"Updated {updatedRecord.Source.Value}.",
            updatedRecord.Id);
    }

    /// <inheritdoc />
    public LocalPassOperationResult DeleteSecret(SecretVaultSession session, SecretRecord secret)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(secret);

        var timestamp = _clock.UtcNow;
        var updatedVault = session.Vault.WithoutEntry(secret.Id, timestamp);
        var updatedSession = _vaultStore.Save(session.WithVault(updatedVault));
        return new LocalPassOperationResult(updatedSession, "Secret deleted.");
    }

    /// <inheritdoc />
    public LocalPassOperationResult ChangeMasterPassword(
        SecretVaultSession session,
        MasterPassword newMasterPassword)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(newMasterPassword);

        var updatedSession = _vaultStore.ChangeMasterPassword(session, newMasterPassword);
        return new LocalPassOperationResult(
            updatedSession,
            "Master password updated and vault re-encrypted.");
    }

    /// <inheritdoc />
    public string OpenStorageDirectory()
    {
        var storageDirectoryPath = _storageLocation.GetStorageDirectoryPath();
        _folderOpener.OpenDirectory(storageDirectoryPath);
        return $"Opened storage directory: {storageDirectoryPath}";
    }

    private readonly IClock _clock;
    private readonly IFolderOpener _folderOpener;
    private readonly ISecretVaultStorageLocation _storageLocation;
    private readonly ISecretVaultStore _vaultStore;
}
