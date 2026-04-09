using Models;

using System.Globalization;
using System.Text;

namespace Infrastructure;

/// <summary>
/// Builds formatted UI text for the LocalPass console views.
/// </summary>
public static class LocalPassViewFormatter
{
    /// <summary>
    /// Builds the list item labels for password entries.
    /// </summary>
    /// <param name="entries">Password entries to format.</param>
    /// <returns>List item labels for the password entries.</returns>
    public static List<string> BuildPasswordListItems(IEnumerable<SecretRecord> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        return [.. entries.Select((entry, index) =>
            $"[{index + 1}] {entry.Source.Value} | {entry.Login.Value}")];
    }

    /// <summary>
    /// Builds the list item labels for secure notes.
    /// </summary>
    /// <param name="notes">Secure notes to format.</param>
    /// <returns>List item labels for the secure notes.</returns>
    public static List<string> BuildNoteListItems(IEnumerable<SecureNoteRecord> notes)
    {
        ArgumentNullException.ThrowIfNull(notes);

        return [.. notes.Select((note, index) =>
            $"[{index + 1}] {note.Title.Value} | {note.Description.Value}")];
    }

    /// <summary>
    /// Builds the summary line for the current vault and selection.
    /// </summary>
    /// <param name="vault">Vault to summarize.</param>
    /// <param name="activeTab">Currently active vault tab.</param>
    /// <param name="selectedLabel">Currently selected label, if any.</param>
    /// <returns>The formatted summary line.</returns>
    public static string BuildSummary(
        SecretVault vault,
        Abstractions.LocalPassVaultTab activeTab,
        string? selectedLabel)
    {
        ArgumentNullException.ThrowIfNull(vault);

        var selectedText = string.IsNullOrWhiteSpace(selectedLabel) ? "none" : selectedLabel;

        return
            $"PASSWORDS {vault.Count:000}  NOTES {vault.NoteCount:000}  ACTIVE {activeTab.ToString().ToUpperInvariant()}  " +
            $"LAST WRITE {vault.UpdatedUtc:yyyy-MM-dd HH:mm} UTC  DOC V{vault.DocumentVersion:000}  TARGET {selectedText}";
    }

    /// <summary>
    /// Builds the details view for the selected secret.
    /// </summary>
    /// <param name="secret">Selected secret, if any.</param>
    /// <param name="revealPasswords">Whether passwords should be visible.</param>
    /// <returns>The formatted details text.</returns>
    public static string BuildPasswordDetails(SecretRecord? secret, bool revealPasswords)
    {
        if (secret is null)
        {
            return "No passwords indexed.\n\nPress N to create the first record.";
        }

        var builder = new StringBuilder();
        _ = builder.AppendLine("SOURCE    " + secret.Source.Value);
        _ = builder.AppendLine("LOGIN     " + secret.Login.Value);
        _ = builder.AppendLine(
            "PASSWORD  " + (revealPasswords ? secret.Password.Value : BuildMaskedPassword(secret.Password.Value)));
        _ = builder.AppendLine("NOTES     " + (secret.Notes.HasValue ? secret.Notes.Value : "(none)"));
        _ = builder.AppendLine("CREATED   " + secret.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC");
        _ = builder.AppendLine("UPDATED   " + secret.UpdatedUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC");
        _ = builder.AppendLine("RECORD ID " + secret.Id);

        return builder.ToString();
    }

    /// <summary>
    /// Builds the details view for the selected secure note.
    /// </summary>
    /// <param name="note">Selected secure note, if any.</param>
    /// <returns>The formatted details text.</returns>
    public static string BuildNoteDetails(SecureNoteRecord? note)
    {
        if (note is null)
        {
            return "No secure notes indexed.\n\nPress N to create the first note.";
        }

        var builder = new StringBuilder();
        _ = builder.AppendLine("TITLE      " + note.Title.Value);
        _ = builder.AppendLine("SUMMARY    " + note.Description.Value);
        _ = builder.AppendLine("CONTENT");
        _ = builder.AppendLine(note.Content.Value);
        _ = builder.AppendLine();
        _ = builder.AppendLine("CREATED    " + note.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC");
        _ = builder.AppendLine("UPDATED    " + note.UpdatedUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC");
        _ = builder.AppendLine("NOTE ID    " + note.Id);

        return builder.ToString();
    }

    /// <summary>
    /// Formats a status message for display in the status bar.
    /// </summary>
    /// <param name="statusMessage">Raw status message.</param>
    /// <returns>The formatted status line.</returns>
    public static string FormatStatus(string statusMessage)
        => "[ READY ] " + (statusMessage ?? string.Empty);

    /// <summary>
    /// Builds the title of the index frame including tab hints.
    /// </summary>
    /// <param name="activeTab">Currently active tab.</param>
    /// <returns>The frame title.</returns>
    public static string BuildIndexTitle(Abstractions.LocalPassVaultTab activeTab)
        => activeTab == Abstractions.LocalPassVaultTab.Passwords
            ? "Vault Index :: [Passwords] | Notes  (Tab switch)"
            : "Vault Index :: Passwords | [Notes]  (Tab switch)";

    /// <summary>
    /// Builds a masked password display value.
    /// </summary>
    /// <param name="password">Password to mask.</param>
    /// <returns>The masked password display string.</returns>
    public static string BuildMaskedPassword(string password)
        => $"{new string('*', Math.Clamp(password?.Length ?? 0, 8, 16))} ({password?.Length ?? 0} chars hidden)";
}
