using Terminal.Gui;

namespace Infrastructure;

/// <summary>
/// Creates the color schemes used by the LocalPass console UI.
/// </summary>
public static class LocalPassConsoleTheme
{
    /// <summary>
    /// Creates the accent color scheme used for labels and read-only content.
    /// </summary>
    /// <returns>The accent color scheme.</returns>
    public static ColorScheme CreateAccentScheme()
        => new()
        {
            Normal = Terminal.Gui.Attribute.Make(Color.Green, Color.Black),
            Focus = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            HotNormal = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            HotFocus = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            Disabled = Terminal.Gui.Attribute.Make(Color.DarkGray, Color.Black)
        };

    /// <summary>
    /// Creates the chrome color scheme used for window frames and borders.
    /// </summary>
    /// <returns>The chrome color scheme.</returns>
    public static ColorScheme CreateChromeScheme()
        => new()
        {
            Normal = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            Focus = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            HotNormal = Terminal.Gui.Attribute.Make(Color.Green, Color.Black),
            HotFocus = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            Disabled = Terminal.Gui.Attribute.Make(Color.DarkGray, Color.Black)
        };

    /// <summary>
    /// Creates the focus color scheme used for input controls.
    /// </summary>
    /// <returns>The focus color scheme.</returns>
    public static ColorScheme CreateFocusScheme()
        => new()
        {
            Normal = Terminal.Gui.Attribute.Make(Color.Green, Color.Black),
            Focus = Terminal.Gui.Attribute.Make(Color.Black, Color.BrightGreen),
            HotNormal = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            HotFocus = Terminal.Gui.Attribute.Make(Color.Black, Color.BrightGreen),
            Disabled = Terminal.Gui.Attribute.Make(Color.DarkGray, Color.Black)
        };
}
