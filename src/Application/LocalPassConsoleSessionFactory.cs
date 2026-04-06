using Abstractions;

using Models;

namespace LocalPass.Application;

/// <summary>
/// Creates application console sessions from unlocked vault sessions.
/// </summary>
public sealed class LocalPassConsoleSessionFactory : ILocalPassConsoleSessionFactory
{
    /// <summary>
    /// Initializes a new console session factory.
    /// </summary>
    /// <param name="vaultStore">Encrypted vault store.</param>
    /// <param name="clock">Clock used for new timestamps.</param>
    /// <param name="storageLocation">Provider for the LocalPass storage directory path.</param>
    /// <param name="folderOpener">Adapter used to open directories in the OS shell.</param>
    public LocalPassConsoleSessionFactory(
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
    public ILocalPassConsoleSession Create(SecretVaultSession session)
        => new LocalPassConsoleSession(session, _vaultStore, _clock, _storageLocation, _folderOpener);

    private readonly IClock _clock;
    private readonly IFolderOpener _folderOpener;
    private readonly ISecretVaultStorageLocation _storageLocation;
    private readonly ISecretVaultStore _vaultStore;
}
