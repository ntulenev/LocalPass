namespace Infrastructure;

/// <summary>
/// Resolves the LocalPass storage directory from configuration or returns the default location.
/// </summary>
public static class VaultStorageDirectoryResolver
{
    /// <summary>
    /// Resolves the configured storage directory or falls back to the default LocalPass location.
    /// Relative paths are resolved against the application base directory.
    /// </summary>
    /// <param name="storageDirectory">Configured storage directory override.</param>
    /// <returns>The absolute storage directory path.</returns>
    public static string ResolveStorageDirectory(string? storageDirectory)
    {
        if (string.IsNullOrWhiteSpace(storageDirectory))
        {
            return GetDefaultStorageDirectoryPath();
        }

        var expandedStorageDirectory = Environment.ExpandEnvironmentVariables(storageDirectory.Trim());

        return Path.IsPathRooted(expandedStorageDirectory)
            ? Path.GetFullPath(expandedStorageDirectory)
            : Path.GetFullPath(expandedStorageDirectory, AppContext.BaseDirectory);
    }

    /// <summary>
    /// Gets the default LocalPass storage directory.
    /// </summary>
    /// <returns>The default absolute storage directory path.</returns>
    public static string GetDefaultStorageDirectoryPath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalPass");
}
