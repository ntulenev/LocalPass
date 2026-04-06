using Abstractions;

using Models;

using LocalPassOperationResult = Abstractions.LocalPassOperationResult;

namespace LocalPass.Application;

/// <summary>
/// Application session used by the interactive console UI.
/// </summary>
public sealed class LocalPassConsoleSession : ILocalPassConsoleSession
{
    /// <summary>
    /// Initializes a new console session.
    /// </summary>
    /// <param name="session">Unlocked vault session.</param>
    /// <param name="vaultStore">Encrypted vault store.</param>
    /// <param name="clock">Clock used for new timestamps.</param>
    /// <param name="storageLocation">Provider for the LocalPass storage directory path.</param>
    /// <param name="folderOpener">Adapter used to open directories in the OS shell.</param>
    public LocalPassConsoleSession(
        SecretVaultSession session,
        ISecretVaultStore vaultStore,
        IClock clock,
        ISecretVaultStorageLocation storageLocation,
        IFolderOpener folderOpener)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _vaultStore = vaultStore ?? throw new ArgumentNullException(nameof(vaultStore));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _storageLocation = storageLocation ?? throw new ArgumentNullException(nameof(storageLocation));
        _folderOpener = folderOpener ?? throw new ArgumentNullException(nameof(folderOpener));
    }

    /// <inheritdoc />
    public SecretVault CurrentVault => _session.Vault;

    /// <inheritdoc />
    public string CurrentStatusMessage => _currentStatusMessage;

    /// <inheritdoc />
    public LocalPassOperationResult AddSecret(SecretEditorInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var timestamp = _clock.UtcNow;
        var record = input.ToRecord(timestamp);
        var updatedVault = _session.Vault.WithEntry(record, timestamp);
        return ApplyUpdatedSession(
            _vaultStore.Save(_session.WithVault(updatedVault)),
            $"Saved {record.Source.Value}.",
            record.Id);
    }

    /// <inheritdoc />
    public LocalPassOperationResult EditSecret(Guid secretId, SecretEditorInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var timestamp = _clock.UtcNow;
        var secret = GetSecret(secretId);
        var updatedRecord = input.ApplyTo(secret, timestamp);
        var updatedVault = _session.Vault.WithEntry(updatedRecord, timestamp);
        return ApplyUpdatedSession(
            _vaultStore.Save(_session.WithVault(updatedVault)),
            $"Updated {updatedRecord.Source.Value}.",
            updatedRecord.Id);
    }

    /// <inheritdoc />
    public LocalPassOperationResult DeleteSecret(Guid secretId)
    {
        var timestamp = _clock.UtcNow;
        var updatedVault = _session.Vault.WithoutEntry(secretId, timestamp);
        return ApplyUpdatedSession(
            _vaultStore.Save(_session.WithVault(updatedVault)),
            "Secret deleted.");
    }

    /// <inheritdoc />
    public LocalPassOperationResult ChangeMasterPassword(MasterPassword newMasterPassword)
    {
        ArgumentNullException.ThrowIfNull(newMasterPassword);

        var updatedSession = _vaultStore.ChangeMasterPassword(_session, newMasterPassword);
        return ApplyUpdatedSession(
            updatedSession,
            "Master password updated and vault re-encrypted.");
    }

    /// <inheritdoc />
    public string OpenStorageDirectory()
    {
        var storageDirectoryPath = _storageLocation.GetStorageDirectoryPath();
        _folderOpener.OpenDirectory(storageDirectoryPath);
        _currentStatusMessage = $"Opened storage directory: {storageDirectoryPath}";
        return _currentStatusMessage;
    }

    private LocalPassOperationResult ApplyUpdatedSession(
        SecretVaultSession updatedSession,
        string statusMessage,
        Guid? preferredSelectionId = null)
    {
        _session = updatedSession;
        _currentStatusMessage = statusMessage;
        return new LocalPassOperationResult(updatedSession.Vault, statusMessage, preferredSelectionId);
    }

    private SecretRecord GetSecret(Guid secretId)
    {
        var index = _session.Vault.FindIndex(secretId);
        return index < 0
            ? throw new InvalidDataException("Secret record was not found.")
            : _session.Vault.GetSecret(index);
    }

    private readonly IClock _clock;
    private readonly IFolderOpener _folderOpener;
    private readonly ISecretVaultStorageLocation _storageLocation;
    private readonly ISecretVaultStore _vaultStore;
    private SecretVaultSession _session;
    private string _currentStatusMessage = string.Empty;
}
