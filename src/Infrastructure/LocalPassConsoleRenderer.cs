using Abstractions;

using Models;

using System.IO;
using System.Text;

using Terminal.Gui;

namespace Infrastructure;

/// <summary>
/// Renders the interactive LocalPass UI by using Terminal.Gui.
/// </summary>
public sealed class LocalPassConsoleRenderer : ISecretVaultConsoleRenderer
{
    /// <summary>
    /// Initializes a new console renderer.
    /// </summary>
    /// <param name="vaultStore">Store used for persistence operations.</param>
    /// <param name="clock">Clock used for timestamps on new and updated secrets.</param>
    /// <param name="storageLocation">Provider for the LocalPass storage directory path.</param>
    /// <param name="folderOpener">Adapter used to open directories in the OS shell.</param>
    public LocalPassConsoleRenderer(
        ISecretVaultStore vaultStore,
        IClock clock,
        ISecretVaultStorageLocation storageLocation,
        IFolderOpener folderOpener)
    {
        _vaultStore = vaultStore ?? throw new ArgumentNullException(nameof(vaultStore));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _storageLocation = storageLocation ?? throw new ArgumentNullException(nameof(storageLocation));
        _folderOpener = folderOpener ?? throw new ArgumentNullException(nameof(folderOpener));
    }

    /// <inheritdoc />
    public Task RunAsync(SecretVaultSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        var currentSession = session;
        var revealPasswords = false;
        var suppressSelectionRefresh = false;
        var currentStatusMessage = string.Empty;

        Application.Init();

        try
        {
            var top = Application.Top;
            var chromeScheme = CreateChromeScheme();
            var accentScheme = CreateAccentScheme();
            var focusScheme = CreateFocusScheme();
            top.ColorScheme = chromeScheme;

            using var window = new Window("LocalPass :: Vault Console")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(1),
                ColorScheme = chromeScheme
            };

            var summaryLabel = new Label(string.Empty)
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                ColorScheme = accentScheme
            };

            var statusLabel = new Label(string.Empty)
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                ColorScheme = focusScheme
            };

            var listFrame = new FrameView("Secret Index")
            {
                X = 0,
                Y = 3,
                Width = Dim.Percent(38),
                Height = Dim.Fill(),
                ColorScheme = chromeScheme
            };

            using var listView = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                CanFocus = true,
                ColorScheme = focusScheme
            };

            var detailsFrame = new FrameView("Payload Inspect")
            {
                X = Pos.Right(listFrame),
                Y = 3,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = chromeScheme
            };

            using var detailsView = new TextView()
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

            listFrame.Add(listView);
            detailsFrame.Add(detailsView);
            window.Add(summaryLabel, statusLabel, listFrame, detailsFrame);
            top.Add(window);

            void RefreshUi(string statusMessage)
            {
                currentStatusMessage = statusMessage;

                var items = currentSession.Vault.Entries
                    .Select((entry, index) => $"[{index + 1}] {entry.Source.Value} | {entry.Login.Value}")
                    .ToList();
                var previousSelection = listView.SelectedItem;
                var nextSelection = items.Count == 0
                    ? 0
                    : Math.Clamp(previousSelection, 0, items.Count - 1);

                listView.SetSource(items);
                if (listView.SelectedItem != nextSelection)
                {
                    suppressSelectionRefresh = true;
                    try
                    {
                        listView.SelectedItem = nextSelection;
                    }
                    finally
                    {
                        suppressSelectionRefresh = false;
                    }
                }

                var selectedSecret = GetSelectedSecret(currentSession.Vault, listView.SelectedItem);
                summaryLabel.Text = BuildSummary(currentSession.Vault, selectedSecret);
                statusLabel.Text = FormatStatus(statusMessage);
                detailsFrame.Title = selectedSecret is null
                    ? "Payload Inspect"
                    : $"Payload :: {selectedSecret.Source.Value}";
                detailsView.Text = BuildDetails(selectedSecret, revealPasswords);
            }

            void SetSelection(Guid id)
            {
                var index = currentSession.Vault.FindIndex(id);
                if (index >= 0)
                {
                    listView.SelectedItem = index;
                }
            }

            void PersistVault(SecretVault updatedVault, string statusMessage, Guid? preferredSelection)
            {
                try
                {
                    currentSession = _vaultStore.Save(currentSession.WithVault(updatedVault));
                    RefreshUi(statusMessage);
                    if (preferredSelection.HasValue)
                    {
                        SetSelection(preferredSelection.Value);
                        RefreshUi(statusMessage);
                    }
                }
                catch (Exception exception) when (
                    exception is IOException
                    or UnauthorizedAccessException
                    or InvalidDataException)
                {
                    MessageBox.ErrorQuery("Save failed", exception.Message, "OK");
                    RefreshUi($"Save failed: {exception.Message}");
                }
            }

            void AddSecret()
            {
                if (!TryShowSecretDialog(null, out var result))
                {
                    return;
                }

                var timestamp = _clock.UtcNow;
                var record = SecretRecord.Create(
                    result.Source,
                    result.Login,
                    result.Password,
                    result.Notes,
                    timestamp);
                var updatedVault = currentSession.Vault.WithEntry(record, timestamp);
                PersistVault(updatedVault, $"Saved {record.Source.Value}.", record.Id);
            }

            void EditSecret()
            {
                var selectedSecret = GetSelectedSecret(currentSession.Vault, listView.SelectedItem);
                if (selectedSecret is null)
                {
                    RefreshUi("No secret selected.");
                    return;
                }

                if (!TryShowSecretDialog(selectedSecret, out var result))
                {
                    return;
                }

                var timestamp = _clock.UtcNow;
                var updatedRecord = selectedSecret.Update(
                    result.Source,
                    result.Login,
                    result.Password,
                    result.Notes,
                    timestamp);
                var updatedVault = currentSession.Vault.WithEntry(updatedRecord, timestamp);
                PersistVault(updatedVault, $"Updated {updatedRecord.Source.Value}.", updatedRecord.Id);
            }

            void DeleteSecret()
            {
                var selectedSecret = GetSelectedSecret(currentSession.Vault, listView.SelectedItem);
                if (selectedSecret is null)
                {
                    RefreshUi("No secret selected.");
                    return;
                }

                var choice = MessageBox.Query(
                    "Delete secret",
                    $"Delete {selectedSecret.Source.Value} / {selectedSecret.Login.Value}?",
                    "Delete",
                    "Cancel");
                if (choice != 0)
                {
                    RefreshUi("Delete cancelled.");
                    return;
                }

                var timestamp = _clock.UtcNow;
                var updatedVault = currentSession.Vault.WithoutEntry(selectedSecret.Id, timestamp);
                PersistVault(updatedVault, "Secret deleted.", null);
            }

            void TogglePasswordVisibility()
            {
                revealPasswords = !revealPasswords;
                RefreshUi(revealPasswords ? "Passwords are visible." : "Passwords are hidden.");
            }

            void ChangeMasterPassword()
            {
                if (!TryShowMasterPasswordDialog(out var newMasterPassword))
                {
                    return;
                }

                try
                {
                    currentSession = _vaultStore.ChangeMasterPassword(currentSession, newMasterPassword);
                    RefreshUi("Master password updated and vault re-encrypted.");
                }
                catch (Exception exception) when (
                    exception is IOException
                    or UnauthorizedAccessException
                    or InvalidDataException)
                {
                    MessageBox.ErrorQuery("Password change failed", exception.Message, "OK");
                    RefreshUi($"Password change failed: {exception.Message}");
                }
            }

            void OpenStorageDirectory()
            {
                try
                {
                    var storageDirectoryPath = _storageLocation.GetStorageDirectoryPath();
                    _folderOpener.OpenDirectory(storageDirectoryPath);
                    RefreshUi($"Opened storage directory: {storageDirectoryPath}");
                }
                catch (Exception exception) when (
                    exception is ArgumentException
                    or DirectoryNotFoundException
                    or InvalidOperationException
                    or System.ComponentModel.Win32Exception)
                {
                    MessageBox.ErrorQuery("Open folder failed", exception.Message, "OK");
                    RefreshUi($"Open folder failed: {exception.Message}");
                }
            }

            listView.SelectedItemChanged += _ =>
            {
                if (suppressSelectionRefresh)
                {
                    return;
                }

                RefreshUi(currentStatusMessage);
            };
            listView.OpenSelectedItem += _ => EditSecret();

            using var statusBar = new StatusBar([
                new StatusItem(Key.N, "~N~ New", () => AddSecret()),
                new StatusItem(Key.E, "~E~ Edit", () => EditSecret()),
                new StatusItem(Key.D, "~D~ Delete", () => DeleteSecret()),
                new StatusItem(Key.O, "~O~ Files", () => OpenStorageDirectory()),
                new StatusItem(Key.P, "~P~ Reveal", () => TogglePasswordVisibility()),
                new StatusItem(Key.R, "~R~ Master", () => ChangeMasterPassword()),
                new StatusItem(Key.Esc, "~Esc~ Exit", () => Application.RequestStop())
            ])
            {
                ColorScheme = chromeScheme
            };

            top.Add(statusBar);
            RefreshUi("N new  E edit  D delete  O files  P reveal  R master key  Esc exit");

            using var registration = cancellationToken.Register(() => Application.RequestStop());
            Application.Run();
        }
        finally
        {
            Application.Shutdown();
        }

        return Task.CompletedTask;
    }

    private static SecretRecord? GetSelectedSecret(SecretVault vault, int index)
        => !vault.HasEntries || index < 0 || index >= vault.Count
            ? null
            : vault.GetSecret(index);

    private static string BuildSummary(SecretVault vault, SecretRecord? selectedSecret)
    {
        var selectedText = selectedSecret is null
            ? "none"
            : $"{selectedSecret.Source.Value} / {selectedSecret.Login.Value}";

        return $"VAULT {vault.Count:000}  LAST WRITE {vault.UpdatedUtc:yyyy-MM-dd HH:mm:ss} UTC  TARGET {selectedText}";
    }

    private static string BuildDetails(SecretRecord? secret, bool revealPasswords)
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
        _ = builder.AppendLine("CREATED   " + secret.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + " UTC");
        _ = builder.AppendLine("UPDATED   " + secret.UpdatedUtc.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + " UTC");
        _ = builder.AppendLine("RECORD ID " + secret.Id);

        return builder.ToString();
    }

    private static string FormatStatus(string statusMessage) => "[ READY ] " + statusMessage;

    private static string BuildMaskedPassword(string password)
        => $"{new string('*', Math.Clamp(password.Length, 8, 16))} ({password.Length} chars hidden)";

    private static bool TryShowSecretDialog(SecretRecord? existingSecret, out SecretDialogResult result)
    {
        var wasAccepted = false;
        var pendingResult = default(SecretDialogResult);

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

        sourceField.ColorScheme = CreateFocusScheme();
        loginField.ColorScheme = CreateFocusScheme();
        passwordField.ColorScheme = CreateFocusScheme();
        notesView.ColorScheme = CreateAccentScheme();

        using var dialog = new Dialog(existingSecret is null ? "Inject Secret" : "Patch Secret", 80, 20)
        {
            ColorScheme = CreateChromeScheme()
        };
        dialog.Add(
            new Label("Source or service")
            {
                X = 1,
                Y = 1,
                ColorScheme = CreateAccentScheme()
            },
            sourceField,
            new Label("Login")
            {
                X = 1,
                Y = 4,
                ColorScheme = CreateAccentScheme()
            },
            loginField,
            new Label("Password")
            {
                X = 1,
                Y = 7,
                ColorScheme = CreateAccentScheme()
            },
            passwordField,
            new Label("Notes")
            {
                X = 1,
                Y = 10,
                ColorScheme = CreateAccentScheme()
            },
            notesView);

        using var saveButton = new Button("Save")
        {
            X = 18,
            Y = 16,
            ColorScheme = CreateFocusScheme()
        };
        using var cancelButton = new Button("Cancel")
        {
            X = 32,
            Y = 16,
            ColorScheme = CreateChromeScheme()
        };

        saveButton.Clicked += () =>
        {
            try
            {
                var source = ReadText(sourceField);
                var login = ReadText(loginField);
                var password = ReadText(passwordField);
                var notes = ReadText(notesView);

                _ = new SecretSource(source);
                _ = new SecretLogin(login);
                _ = new SecretPassword(password);

                pendingResult = new SecretDialogResult(source, login, password, notes);
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
        result = pendingResult;
        return wasAccepted;
    }

    private static bool TryShowMasterPasswordDialog(out MasterPassword masterPassword)
    {
        var wasAccepted = false;
        var pendingMasterPassword = default(MasterPassword);

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

        passwordField.ColorScheme = CreateFocusScheme();
        confirmationField.ColorScheme = CreateFocusScheme();

        using var dialog = new Dialog("Rotate Master Key", 80, 14)
        {
            ColorScheme = CreateChromeScheme()
        };
        dialog.Add(
            new Label("New master password")
            {
                X = 1,
                Y = 1,
                ColorScheme = CreateAccentScheme()
            },
            passwordField,
            new Label("Confirm master password")
            {
                X = 1,
                Y = 4,
                ColorScheme = CreateAccentScheme()
            },
            confirmationField,
            new Label("16+ chars, uppercase, lowercase, digit, symbol, no whitespace")
            {
                X = 1,
                Y = 8,
                Width = Dim.Fill(2),
                ColorScheme = CreateAccentScheme()
            });

        using var saveButton = new Button("Save")
        {
            X = 18,
            Y = 10,
            ColorScheme = CreateFocusScheme()
        };
        using var cancelButton = new Button("Cancel")
        {
            X = 32,
            Y = 10,
            ColorScheme = CreateChromeScheme()
        };

        saveButton.Clicked += () =>
        {
            var password = ReadText(passwordField);
            var confirmation = ReadText(confirmationField);
            if (!string.Equals(password, confirmation, StringComparison.Ordinal))
            {
                MessageBox.ErrorQuery("Validation error", "Passwords do not match.", "OK");
                return;
            }

            try
            {
                pendingMasterPassword = new MasterPassword(password);
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
        masterPassword = pendingMasterPassword!;
        return wasAccepted;
    }

    private static string ReadText(TextField field)
        => field.Text?.ToString() ?? string.Empty;

    private static string ReadText(TextView field)
        => field.Text?.ToString() ?? string.Empty;

    private static ColorScheme CreateAccentScheme()
        => new()
        {
            Normal = Terminal.Gui.Attribute.Make(Color.Green, Color.Black),
            Focus = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            HotNormal = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            HotFocus = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            Disabled = Terminal.Gui.Attribute.Make(Color.DarkGray, Color.Black)
        };

    private static ColorScheme CreateChromeScheme()
        => new()
        {
            Normal = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            Focus = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            HotNormal = Terminal.Gui.Attribute.Make(Color.Green, Color.Black),
            HotFocus = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            Disabled = Terminal.Gui.Attribute.Make(Color.DarkGray, Color.Black)
        };

    private static ColorScheme CreateFocusScheme()
        => new()
        {
            Normal = Terminal.Gui.Attribute.Make(Color.Green, Color.Black),
            Focus = Terminal.Gui.Attribute.Make(Color.Black, Color.BrightGreen),
            HotNormal = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
            HotFocus = Terminal.Gui.Attribute.Make(Color.Black, Color.BrightGreen),
            Disabled = Terminal.Gui.Attribute.Make(Color.DarkGray, Color.Black)
        };

    private readonly IClock _clock;
    private readonly IFolderOpener _folderOpener;
    private readonly ISecretVaultStorageLocation _storageLocation;
    private readonly ISecretVaultStore _vaultStore;

    private readonly record struct SecretDialogResult(
        string Source,
        string Login,
        string Password,
        string Notes);
}
