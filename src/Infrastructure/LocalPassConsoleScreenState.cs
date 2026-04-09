using Abstractions;

namespace Infrastructure;

/// <summary>
/// Represents the complete state needed to render the console screen.
/// </summary>
public sealed class LocalPassConsoleScreenState
{
    /// <summary>
    /// Initializes a new screen state snapshot.
    /// </summary>
    /// <param name="items">List items displayed in the vault index.</param>
    /// <param name="selectedIndex">Zero-based selected item index.</param>
    /// <param name="activeTab">Currently active vault tab.</param>
    /// <param name="summaryText">Summary label text.</param>
    /// <param name="statusText">Status label text.</param>
    /// <param name="indexTitle">Index frame title.</param>
    /// <param name="detailsTitle">Details frame title.</param>
    /// <param name="detailsText">Details text.</param>
    public LocalPassConsoleScreenState(
        IReadOnlyList<string> items,
        int selectedIndex,
        LocalPassVaultTab activeTab,
        string summaryText,
        string statusText,
        string indexTitle,
        string detailsTitle,
        string detailsText)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        SelectedIndex = selectedIndex;
        ActiveTab = activeTab;
        SummaryText = summaryText ?? throw new ArgumentNullException(nameof(summaryText));
        StatusText = statusText ?? throw new ArgumentNullException(nameof(statusText));
        IndexTitle = indexTitle ?? throw new ArgumentNullException(nameof(indexTitle));
        DetailsTitle = detailsTitle ?? throw new ArgumentNullException(nameof(detailsTitle));
        DetailsText = detailsText ?? throw new ArgumentNullException(nameof(detailsText));
    }

    /// <summary>
    /// Gets the list items displayed in the vault index.
    /// </summary>
    public IReadOnlyList<string> Items { get; }

    /// <summary>
    /// Gets the selected item index after refresh.
    /// </summary>
    public int SelectedIndex { get; }

    /// <summary>
    /// Gets the active vault tab.
    /// </summary>
    public LocalPassVaultTab ActiveTab { get; }

    /// <summary>
    /// Gets the summary label text.
    /// </summary>
    public string SummaryText { get; }

    /// <summary>
    /// Gets the status label text.
    /// </summary>
    public string StatusText { get; }

    /// <summary>
    /// Gets the index frame title.
    /// </summary>
    public string IndexTitle { get; }

    /// <summary>
    /// Gets the details frame title.
    /// </summary>
    public string DetailsTitle { get; }

    /// <summary>
    /// Gets the details text.
    /// </summary>
    public string DetailsText { get; }
}
