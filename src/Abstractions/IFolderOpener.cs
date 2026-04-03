namespace Abstractions;

/// <summary>
/// Opens a directory in the operating system shell.
/// </summary>
public interface IFolderOpener
{
    /// <summary>
    /// Opens the supplied directory path in the operating system shell.
    /// </summary>
    /// <param name="directoryPath">Absolute directory path to open.</param>
    void OpenDirectory(string directoryPath);
}
