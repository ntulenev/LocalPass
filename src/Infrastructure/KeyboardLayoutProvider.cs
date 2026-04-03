using System.Globalization;
using System.Runtime.InteropServices;

namespace Infrastructure;

/// <summary>
/// Provides the current keyboard layout name for the active window.
/// </summary>
public sealed class KeyboardLayoutProvider
{
    /// <summary>
    /// Initializes a new keyboard layout provider.
    /// </summary>
    public KeyboardLayoutProvider()
        : this(GetCurrentDisplayName)
    {
    }

    internal KeyboardLayoutProvider(Func<string> displayNameProvider)
    {
        _displayNameProvider = displayNameProvider
            ?? throw new ArgumentNullException(nameof(displayNameProvider));
    }

    /// <summary>
    /// Gets the current keyboard layout display name.
    /// </summary>
    /// <returns>The current keyboard layout display name.</returns>
    public string GetDisplayName() => _displayNameProvider();

    private static string GetCurrentDisplayName()
    {
        if (!OperatingSystem.IsWindows())
        {
            return CultureInfo.CurrentCulture.Name;
        }

        try
        {
            var foregroundWindow = GetForegroundWindow();
            var threadIdentifier = foregroundWindow == nint.Zero
                ? 0U
                : GetWindowThreadProcessId(foregroundWindow, nint.Zero);
            var keyboardLayout = GetKeyboardLayout(threadIdentifier);
            var languageIdentifier = unchecked((ushort)keyboardLayout.ToInt64());
            return CultureInfo.GetCultureInfo(languageIdentifier).Name;
        }
        catch (CultureNotFoundException)
        {
            return "unknown";
        }
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("user32.dll")]
    private static extern nint GetKeyboardLayout(uint idThread);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint windowHandle, nint processId);

    private readonly Func<string> _displayNameProvider;
}
