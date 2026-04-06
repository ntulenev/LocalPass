using Abstractions;

using Models;

using System.IO;
using System.Text;

namespace Infrastructure;

/// <summary>
/// Stores the LocalPass vault in a locally encrypted file.
/// </summary>
public sealed class FileSecretVaultStore : ISecretVaultStore, ISecretVaultStorageLocation
{
    /// <summary>
    /// Initializes a store that uses the default LocalPass storage location.
    /// </summary>
    /// <param name="clock">Clock used for vault timestamps and snapshot naming.</param>
    public FileSecretVaultStore(IClock clock)
        : this(VaultStorageDirectoryResolver.GetDefaultStorageDirectoryPath(), clock)
    {
    }

    /// <summary>
    /// Initializes a store that writes to a specific storage directory.
    /// </summary>
    /// <param name="storageDirectory">Directory containing the encrypted vault and snapshots.</param>
    /// <param name="clock">Clock used for vault timestamps and snapshot naming.</param>
    public FileSecretVaultStore(string storageDirectory, IClock clock)
    {
        _storageLayout = new VaultStorageLayout(storageDirectory, clock);
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc />
    public bool Exists() => File.Exists(_storageLayout.VaultFilePath);

    /// <inheritdoc />
    public SecretVaultSession CreateNew(MasterPassword masterPassword)
    {
        ArgumentNullException.ThrowIfNull(masterPassword);

        var vault = SecretVault.CreateEmpty(_clock.UtcNow);
        return Save(new SecretVaultSession(vault, masterPassword));
    }

    /// <inheritdoc />
    public SecretVaultSession Open(MasterPassword masterPassword)
    {
        ArgumentNullException.ThrowIfNull(masterPassword);

        if (!File.Exists(_storageLayout.VaultFilePath))
        {
            throw new FileNotFoundException("Vault file was not found.", _storageLayout.VaultFilePath);
        }

        var json = File.ReadAllText(_storageLayout.VaultFilePath, Encoding.UTF8);
        var document = VaultEnvelopeSerializer.Deserialize(json);
        var vault = VaultCryptographyService.DecryptVault(document, masterPassword);
        return new SecretVaultSession(vault, masterPassword);
    }

    /// <inheritdoc />
    public SecretVaultSession Save(SecretVaultSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        Directory.CreateDirectory(_storageLayout.StorageDirectoryPath);
        Directory.CreateDirectory(_storageLayout.SnapshotDirectoryPath);

        var document = VaultCryptographyService.EncryptVault(session.Vault, session.MasterPassword);
        var json = VaultEnvelopeSerializer.Serialize(document);
        var temporaryPath = _storageLayout.BuildTemporaryFilePath();

        try
        {
            WriteAllText(temporaryPath, json);

            if (File.Exists(_storageLayout.VaultFilePath))
            {
                var snapshotPath = _storageLayout.BuildSnapshotPath();
                File.Replace(temporaryPath, _storageLayout.VaultFilePath, snapshotPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, _storageLayout.VaultFilePath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return session;
    }

    /// <inheritdoc />
    public SecretVaultSession ChangeMasterPassword(
        SecretVaultSession session,
        MasterPassword newMasterPassword)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(newMasterPassword);

        return Save(session.WithMasterPassword(newMasterPassword));
    }

    /// <inheritdoc />
    public string GetStorageDirectoryPath() => _storageLayout.StorageDirectoryPath;

    private static void WriteAllText(string path, string content)
    {
        using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.WriteThrough);
        using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
        writer.Flush();
        stream.Flush(flushToDisk: true);
    }

    private readonly IClock _clock;
    private readonly VaultStorageLayout _storageLayout;
}
