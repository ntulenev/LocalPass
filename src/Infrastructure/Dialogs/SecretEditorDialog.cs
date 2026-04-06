using Models;

using System.IO;

using Terminal.Gui;

namespace Infrastructure.Dialogs;

/// <summary>
/// Modal dialog used to create or edit a secret entry.
/// </summary>
public static class SecretEditorDialog
{
    /// <summary>
    /// Gets the dialog title for the supplied secret state.
    /// </summary>
    /// <param name="existingSecret">Existing secret, if editing.</param>
    /// <returns>The dialog title.</returns>
    public static string GetTitle(SecretRecord? existingSecret)
        => existingSecret is null ? "Inject Secret" : "Patch Secret";

    /// <summary>
    /// Creates validated editor input from raw dialog values.
    /// </summary>
    /// <param name="source">Raw source value.</param>
    /// <param name="login">Raw login value.</param>
    /// <param name="password">Raw password value.</param>
    /// <param name="notes">Raw notes value.</param>
    /// <returns>Validated input for a secret record.</returns>
    public static SecretEditorInput CreateValidatedInput(
        string? source,
        string? login,
        string? password,
        string? notes)
        => SecretEditorInput.Create(source, login, password, notes);

    /// <summary>
    /// Shows the secret editor dialog and returns validated input when accepted.
    /// </summary>
    /// <param name="existingSecret">Existing secret, if editing.</param>
    /// <returns>Validated editor input, or <see langword="null"/> when cancelled.</returns>
    public static SecretEditorInput? Prompt(SecretRecord? existingSecret)
    {
        var wasAccepted = false;
        SecretEditorInput? pendingResult = null;

        var sourceField = new TextField(existingSecret?.Source.Value ?? string.Empty)
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2)
        };
        var loginField = new TextField(existingSecret?.Login.Value ?? string.Empty)
        {
            X = 1,
            Y = 5,
            Width = Dim.Fill(2)
        };
        var passwordField = new TextField(existingSecret?.Password.Value ?? string.Empty)
        {
            X = 1,
            Y = 8,
            Width = Dim.Fill(2),
            Secret = true
        };
        var notesView = new TextView()
        {
            X = 1,
            Y = 11,
            Width = Dim.Fill(2),
            Height = 4,
            Text = existingSecret?.Notes.Value ?? string.Empty
        };

        sourceField.ColorScheme = LocalPassConsoleTheme.CreateFocusScheme();
        loginField.ColorScheme = LocalPassConsoleTheme.CreateFocusScheme();
        passwordField.ColorScheme = LocalPassConsoleTheme.CreateFocusScheme();
        notesView.ColorScheme = LocalPassConsoleTheme.CreateAccentScheme();

        using var dialog = new Dialog(GetTitle(existingSecret), 80, 20)
        {
            ColorScheme = LocalPassConsoleTheme.CreateChromeScheme()
        };
        dialog.Add(
            new Label("Source or service")
            {
                X = 1,
                Y = 1,
                ColorScheme = LocalPassConsoleTheme.CreateAccentScheme()
            },
            sourceField,
            new Label("Login")
            {
                X = 1,
                Y = 4,
                ColorScheme = LocalPassConsoleTheme.CreateAccentScheme()
            },
            loginField,
            new Label("Password")
            {
                X = 1,
                Y = 7,
                ColorScheme = LocalPassConsoleTheme.CreateAccentScheme()
            },
            passwordField,
            new Label("Notes")
            {
                X = 1,
                Y = 10,
                ColorScheme = LocalPassConsoleTheme.CreateAccentScheme()
            },
            notesView);

        using var saveButton = new Button("Save")
        {
            X = 18,
            Y = 16,
            ColorScheme = LocalPassConsoleTheme.CreateFocusScheme()
        };
        using var cancelButton = new Button("Cancel")
        {
            X = 32,
            Y = 16,
            ColorScheme = LocalPassConsoleTheme.CreateChromeScheme()
        };

        saveButton.Clicked += () =>
        {
            try
            {
                var source = ReadText(sourceField);
                var login = ReadText(loginField);
                var password = ReadText(passwordField);
                var notes = ReadText(notesView);

                pendingResult = CreateValidatedInput(source, login, password, notes);
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
        return wasAccepted ? pendingResult : null;
    }

    private static string ReadText(TextField field)
        => field.Text?.ToString() ?? string.Empty;

    private static string ReadText(TextView field)
        => field.Text?.ToString() ?? string.Empty;
}
