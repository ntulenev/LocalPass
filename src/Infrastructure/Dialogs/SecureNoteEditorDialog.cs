using Models;

using System.IO;

using Terminal.Gui;

namespace Infrastructure.Dialogs;

/// <summary>
/// Modal dialog used to create or edit a secure note entry.
/// </summary>
public static class SecureNoteEditorDialog
{
    /// <summary>
    /// Gets the dialog title for the supplied note state.
    /// </summary>
    /// <param name="existingNote">Existing note, if editing.</param>
    /// <returns>The dialog title.</returns>
    public static string GetTitle(SecureNoteRecord? existingNote)
        => existingNote is null ? "Create Note" : "Edit Note";

    /// <summary>
    /// Creates validated editor input from raw dialog values.
    /// </summary>
    /// <param name="title">Raw title value.</param>
    /// <param name="description">Raw description value.</param>
    /// <param name="content">Raw content value.</param>
    /// <returns>Validated input for a secure note record.</returns>
    public static SecureNoteEditorInput CreateValidatedInput(
        string? title,
        string? description,
        string? content)
        => SecureNoteEditorInput.Create(title, description, content);

    /// <summary>
    /// Shows the secure note editor dialog and returns validated input when accepted.
    /// </summary>
    /// <param name="existingNote">Existing note, if editing.</param>
    /// <returns>Validated editor input, or <see langword="null"/> when cancelled.</returns>
    public static SecureNoteEditorInput? Prompt(SecureNoteRecord? existingNote)
    {
        var wasAccepted = false;
        SecureNoteEditorInput? pendingResult = null;

        var titleField = new TextField(existingNote?.Title.Value ?? string.Empty)
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2)
        };
        var descriptionField = new TextField(existingNote?.Description.Value ?? string.Empty)
        {
            X = 1,
            Y = 5,
            Width = Dim.Fill(2)
        };
        var contentView = new TextView()
        {
            X = 1,
            Y = 8,
            Width = Dim.Fill(2),
            Height = 10,
            Text = existingNote?.Content.Value ?? string.Empty
        };

        titleField.ColorScheme = LocalPassConsoleTheme.CreateFocusScheme();
        descriptionField.ColorScheme = LocalPassConsoleTheme.CreateFocusScheme();
        contentView.ColorScheme = LocalPassConsoleTheme.CreateAccentScheme();

        using var dialog = new Dialog(GetTitle(existingNote), 90, 24)
        {
            ColorScheme = LocalPassConsoleTheme.CreateChromeScheme()
        };
        dialog.Add(
            new Label("Title")
            {
                X = 1,
                Y = 1,
                ColorScheme = LocalPassConsoleTheme.CreateAccentScheme()
            },
            titleField,
            new Label("Short description")
            {
                X = 1,
                Y = 4,
                ColorScheme = LocalPassConsoleTheme.CreateAccentScheme()
            },
            descriptionField,
            new Label("Encrypted content")
            {
                X = 1,
                Y = 7,
                ColorScheme = LocalPassConsoleTheme.CreateAccentScheme()
            },
            contentView);

        using var saveButton = new Button("Save")
        {
            X = 24,
            Y = 19,
            ColorScheme = LocalPassConsoleTheme.CreateFocusScheme()
        };
        using var cancelButton = new Button("Cancel")
        {
            X = 38,
            Y = 19,
            ColorScheme = LocalPassConsoleTheme.CreateChromeScheme()
        };

        saveButton.Clicked += () =>
        {
            try
            {
                var title = ReadText(titleField);
                var description = ReadText(descriptionField);
                var content = ReadText(contentView);

                pendingResult = CreateValidatedInput(title, description, content);
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
