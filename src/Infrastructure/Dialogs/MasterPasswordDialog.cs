using Models;

using System.IO;

using Terminal.Gui;

namespace Infrastructure.Dialogs;

/// <summary>
/// Modal dialog used to rotate the master password.
/// </summary>
public static class MasterPasswordDialog
{
    private const string RequirementsTextValue = "16+ chars, uppercase, lowercase, digit, symbol, no whitespace";

    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    public static string Title => "Rotate Master Key";

    /// <summary>
    /// Gets the requirements text rendered by the dialog.
    /// </summary>
    public static string RequirementsText => RequirementsTextValue;

    /// <summary>
    /// Creates a validated master password from raw dialog values.
    /// </summary>
    /// <param name="password">Raw password value.</param>
    /// <param name="confirmation">Raw confirmation value.</param>
    /// <returns>The validated master password.</returns>
    public static MasterPassword CreateValidatedMasterPassword(string? password, string? confirmation)
    {
        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            throw new InvalidDataException("Passwords do not match.");
        }

        return new MasterPassword(password);
    }

    /// <summary>
    /// Shows the master password rotation dialog and returns the validated password when accepted.
    /// </summary>
    /// <returns>The validated master password, or <see langword="null"/> when cancelled.</returns>
    public static MasterPassword? Prompt()
    {
        var wasAccepted = false;
        MasterPassword? pendingMasterPassword = null;

        var passwordField = new TextField(string.Empty)
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2),
            Secret = true
        };
        var confirmationField = new TextField(string.Empty)
        {
            X = 1,
            Y = 5,
            Width = Dim.Fill(2),
            Secret = true
        };

        passwordField.ColorScheme = LocalPassConsoleTheme.CreateFocusScheme();
        confirmationField.ColorScheme = LocalPassConsoleTheme.CreateFocusScheme();

        using var dialog = new Dialog(Title, 80, 14)
        {
            ColorScheme = LocalPassConsoleTheme.CreateChromeScheme()
        };
        dialog.Add(
            new Label("New master password")
            {
                X = 1,
                Y = 1,
                ColorScheme = LocalPassConsoleTheme.CreateAccentScheme()
            },
            passwordField,
            new Label("Confirm master password")
            {
                X = 1,
                Y = 4,
                ColorScheme = LocalPassConsoleTheme.CreateAccentScheme()
            },
            confirmationField,
            new Label(RequirementsText)
            {
                X = 1,
                Y = 8,
                Width = Dim.Fill(2),
                ColorScheme = LocalPassConsoleTheme.CreateAccentScheme()
            });

        using var saveButton = new Button("Save")
        {
            X = 18,
            Y = 10,
            ColorScheme = LocalPassConsoleTheme.CreateFocusScheme()
        };
        using var cancelButton = new Button("Cancel")
        {
            X = 32,
            Y = 10,
            ColorScheme = LocalPassConsoleTheme.CreateChromeScheme()
        };

        saveButton.Clicked += () =>
        {
            var password = ReadText(passwordField);
            var confirmation = ReadText(confirmationField);
            try
            {
                pendingMasterPassword = CreateValidatedMasterPassword(password, confirmation);
                wasAccepted = true;
                Application.RequestStop();
            }
            catch (InvalidDataException exception)
            {
                MessageBox.ErrorQuery("Validation error", exception.Message, "OK");
            }
        };

        cancelButton.Clicked += () => Application.RequestStop();
        dialog.AddButton(saveButton);
        dialog.AddButton(cancelButton);

        Application.Run(dialog);
        return wasAccepted ? pendingMasterPassword : null;
    }

    private static string ReadText(TextField field)
        => field.Text?.ToString() ?? string.Empty;
}
