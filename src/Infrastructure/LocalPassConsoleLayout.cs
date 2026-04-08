using Terminal.Gui;

namespace Infrastructure;

/// <summary>
/// Creates the static Terminal.Gui controls used by the LocalPass console UI.
/// </summary>
public static class LocalPassConsoleLayout
{
    /// <summary>
    /// Creates the top-level application window.
    /// </summary>
    /// <param name="chromeScheme">Color scheme for non-focused chrome.</param>
    /// <returns>The configured window.</returns>
    public static Window CreateWindow(ColorScheme chromeScheme)
        => new("LocalPass :: Vault Console")
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            ColorScheme = chromeScheme
        };

    /// <summary>
    /// Creates the vault summary label.
    /// </summary>
    /// <param name="accentScheme">Color scheme for read-only accent content.</param>
    /// <returns>The configured label.</returns>
    public static Label CreateSummaryLabel(ColorScheme accentScheme)
        => new(string.Empty)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            ColorScheme = accentScheme
        };

    /// <summary>
    /// Creates the status label.
    /// </summary>
    /// <param name="focusScheme">Color scheme for focused chrome.</param>
    /// <returns>The configured label.</returns>
    public static Label CreateStatusLabel(ColorScheme focusScheme)
        => new(string.Empty)
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            ColorScheme = focusScheme
        };

    /// <summary>
    /// Creates the left-hand secret index frame.
    /// </summary>
    /// <param name="chromeScheme">Color scheme for non-focused chrome.</param>
    /// <returns>The configured frame view.</returns>
    public static FrameView CreateSecretIndexFrame(ColorScheme chromeScheme)
        => new("Secret Index")
        {
            X = 0,
            Y = 3,
            Width = Dim.Percent(38),
            Height = Dim.Fill(),
            ColorScheme = chromeScheme
        };

    /// <summary>
    /// Creates the list view used to show vault entries.
    /// </summary>
    /// <param name="focusScheme">Color scheme for input controls.</param>
    /// <returns>The configured list view.</returns>
    public static ListView CreateSecretListView(ColorScheme focusScheme)
        => new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            ColorScheme = focusScheme
        };

    /// <summary>
    /// Creates the right-hand details frame.
    /// </summary>
    /// <param name="chromeScheme">Color scheme for non-focused chrome.</param>
    /// <param name="secretIndexFrame">The left-hand frame that the details view anchors against.</param>
    /// <returns>The configured frame view.</returns>
    public static FrameView CreatePayloadInspectFrame(
        ColorScheme chromeScheme,
        FrameView secretIndexFrame)
    {
        ArgumentNullException.ThrowIfNull(secretIndexFrame);

        return new FrameView("Payload Inspect")
        {
            X = Pos.Right(secretIndexFrame),
            Y = 3,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = chromeScheme
        };
    }

    /// <summary>
    /// Creates the read-only details view.
    /// </summary>
    /// <param name="accentScheme">Color scheme for read-only accent content.</param>
    /// <returns>The configured text view.</returns>
    public static TextView CreatePayloadDetailsView(ColorScheme accentScheme)
        => new()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
            CanFocus = false,
            ColorScheme = accentScheme
        };
}
