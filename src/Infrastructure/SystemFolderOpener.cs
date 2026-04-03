using Abstractions;

using System.Diagnostics;
using System.IO;

namespace Infrastructure;

/// <summary>
/// Opens directories by using the operating system shell.
/// </summary>
public sealed class SystemFolderOpener : IFolderOpener
{
    /// <inheritdoc />
    public void OpenDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Directory path is required.", nameof(directoryPath));
        }

        var fullPath = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Directory was not found: {fullPath}");
        }

        _ = Process.Start(new ProcessStartInfo
        {
            FileName = fullPath,
            UseShellExecute = true
        });
    }
}
