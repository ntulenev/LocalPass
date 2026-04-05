using Infrastructure;

using Microsoft.Extensions.Configuration;

namespace LocalPass.Utility;

/// <summary>
/// Resolves the LocalPass vault storage directory from application configuration.
/// </summary>
public static class StorageDirectoryConfiguration
{
    /// <summary>
    /// Configuration key that overrides the vault storage directory.
    /// </summary>
    public const string StorageDirectoryPathKey = "LocalPass:StorageDirectoryPath";

    /// <summary>
    /// Resolves the configured storage directory or returns the LocalPass default location.
    /// </summary>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The absolute storage directory path.</returns>
    public static string ResolveStorageDirectoryPath(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return FileSecretVaultStore.ResolveStorageDirectory(configuration[StorageDirectoryPathKey]);
    }
}
