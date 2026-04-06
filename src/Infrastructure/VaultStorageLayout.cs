using Abstractions;

using System.Globalization;

namespace Infrastructure;

/// <summary>
/// Encapsulates the vault file layout inside the storage directory.
/// </summary>
public sealed class VaultStorageLayout
{
    private const string SnapshotDirectoryName = "snapshots";
    private const string VaultFileName = "vault.localpass";

    /// <summary>
    /// Initializes a new storage layout for the vault.
    /// </summary>
    /// <param name="storageDirectory">Directory containing the encrypted vault and snapshots.</param>
    /// <param name="clock">Clock used for snapshot naming.</param>
    public VaultStorageLayout(string storageDirectory, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(storageDirectory))
        {
            throw new ArgumentException("Storage directory is required.", nameof(storageDirectory));
        }

        StorageDirectoryPath = Path.GetFullPath(storageDirectory);
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// Gets the absolute storage directory path.
    /// </summary>
    public string StorageDirectoryPath { get; }

    /// <summary>
    /// Gets the vault file path.
    /// </summary>
    public string VaultFilePath => Path.Combine(StorageDirectoryPath, VaultFileName);

    /// <summary>
    /// Gets the snapshot directory path.
    /// </summary>
    public string SnapshotDirectoryPath => Path.Combine(StorageDirectoryPath, SnapshotDirectoryName);

    /// <summary>
    /// Builds a unique temporary file path in the storage directory.
    /// </summary>
    /// <returns>The temporary file path.</returns>
    public string BuildTemporaryFilePath()
        => Path.Combine(StorageDirectoryPath, $"{Guid.NewGuid():N}.tmp");

    /// <summary>
    /// Builds a snapshot path for replacing an existing vault file.
    /// </summary>
    /// <returns>The snapshot file path.</returns>
    public string BuildSnapshotPath()
    {
        var timestamp = _clock.UtcNow.ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture);
        return Path.Combine(SnapshotDirectoryPath, $"vault-{timestamp}-{Guid.NewGuid():N}.localpass");
    }

    private readonly IClock _clock;
}
