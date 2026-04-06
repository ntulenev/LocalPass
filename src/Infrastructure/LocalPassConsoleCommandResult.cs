namespace Infrastructure;

/// <summary>
/// Represents the outcome of a console command handled by the controller.
/// </summary>
public sealed class LocalPassConsoleCommandResult
{
    private LocalPassConsoleCommandResult(
        bool shouldRefresh,
        Guid? preferredSelectionId = null,
        string? errorDialogTitle = null,
        string? errorDialogMessage = null)
    {
        ShouldRefresh = shouldRefresh;
        PreferredSelectionId = preferredSelectionId;
        ErrorDialogTitle = errorDialogTitle;
        ErrorDialogMessage = errorDialogMessage;
    }

    /// <summary>
    /// Gets a value indicating whether the UI should be refreshed.
    /// </summary>
    public bool ShouldRefresh { get; }

    /// <summary>
    /// Gets the optional selection that should be applied after refresh.
    /// </summary>
    public Guid? PreferredSelectionId { get; }

    /// <summary>
    /// Gets the optional error dialog title.
    /// </summary>
    public string? ErrorDialogTitle { get; }

    /// <summary>
    /// Gets the optional error dialog message.
    /// </summary>
    public string? ErrorDialogMessage { get; }

    /// <summary>
    /// Creates a result that leaves the UI unchanged.
    /// </summary>
    /// <returns>The no-op result.</returns>
    public static LocalPassConsoleCommandResult NoChange() => new(shouldRefresh: false);

    /// <summary>
    /// Creates a result that refreshes the UI.
    /// </summary>
    /// <param name="preferredSelectionId">Optional preferred selection after refresh.</param>
    /// <returns>The refresh result.</returns>
    public static LocalPassConsoleCommandResult Refresh(Guid? preferredSelectionId = null)
        => new(shouldRefresh: true, preferredSelectionId: preferredSelectionId);

    /// <summary>
    /// Creates a result that refreshes the UI and shows an error dialog.
    /// </summary>
    /// <param name="title">Error dialog title.</param>
    /// <param name="message">Error dialog message.</param>
    /// <returns>The error result.</returns>
    public static LocalPassConsoleCommandResult Error(string title, string message)
        => new(
            shouldRefresh: true,
            errorDialogTitle: string.IsNullOrWhiteSpace(title)
                ? throw new ArgumentException("Error title is required.", nameof(title))
                : title,
            errorDialogMessage: string.IsNullOrWhiteSpace(message)
                ? throw new ArgumentException("Error message is required.", nameof(message))
                : message);
}
