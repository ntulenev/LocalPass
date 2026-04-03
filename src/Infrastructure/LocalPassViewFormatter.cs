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
    /// Builds the list item labels for vault entries.
    /// </summary>
    /// <param name="vault">Vault to format.</param>
    /// <returns>List item labels for the vault entries.</returns>
    public static List<string> BuildListItems(SecretVault vault)
    {
        ArgumentNullException.ThrowIfNull(vault);

        return [.. vault.Entries.Select((entry, index) =>
            $"[{index + 1}] {entry.Source.Value} | {entry.Login.Value}")];
    }

    /// <summary>
    /// Builds the summary line for the current vault and selection.
    /// </summary>
    /// <param name="vault">Vault to summarize.</param>
    /// <param name="selectedSecret">Currently selected secret, if any.</param>
    /// <returns>The formatted summary line.</returns>
    public static string BuildSummary(SecretVault vault, SecretRecord? selectedSecret)
    {
        ArgumentNullException.ThrowIfNull(vault);

        var selectedText = selectedSecret is null
            ? "none"
            : $"{selectedSecret.Source.Value} / {selectedSecret.Login.Value}";

        return $"VAULT {vault.Count:000}  LAST WRITE {vault.UpdatedUtc:yyyy-MM-dd HH:mm:ss} UTC  TARGET {selectedText}";
    }

    /// <summary>
    /// Builds the details view for the selected secret.
    /// </summary>
    /// <param name="secret">Selected secret, if any.</param>
    /// <param name="revealPasswords">Whether passwords should be visible.</param>
    /// <returns>The formatted details text.</returns>
    public static string BuildDetails(SecretRecord? secret, bool revealPasswords)
    {
        if (secret is null)
        {
            return "No secrets indexed.\n\nPress N to create the first record.";
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
    /// Formats a status message for display in the status bar.
    /// </summary>
    /// <param name="statusMessage">Raw status message.</param>
    /// <returns>The formatted status line.</returns>
    public static string FormatStatus(string statusMessage)
        => "[ READY ] " + (statusMessage ?? string.Empty);

    /// <summary>
    /// Builds a masked password display value.
    /// </summary>
    /// <param name="password">Password to mask.</param>
    /// <returns>The masked password display string.</returns>
    public static string BuildMaskedPassword(string password)
        => $"{new string('*', Math.Clamp(password?.Length ?? 0, 8, 16))} ({password?.Length ?? 0} chars hidden)";
}
