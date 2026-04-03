namespace Abstractions;

/// <summary>
/// Provides the storage directory used by the encrypted vault.
/// </summary>
public interface ISecretVaultStorageLocation
{
    /// <summary>
    /// Gets the absolute storage directory path.
    /// </summary>
    /// <returns>The absolute storage directory path.</returns>
    string GetStorageDirectoryPath();
}
