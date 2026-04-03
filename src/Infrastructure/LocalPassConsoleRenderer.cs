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
    public LocalPassConsoleRenderer(ISecretVaultStore vaultStore, IClock clock)
    {
        _vaultStore = vaultStore ?? throw new ArgumentNullException(nameof(vaultStore));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc />
    public Task RunAsync(SecretVaultSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        var currentSession = session;
        var revealPasswords = false;
        var suppressSelectionRefresh = false;

        Application.Init();

        try
        {
            var top = Application.Top;

            using var window = new Window("LocalPass")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };

            var summaryLabel = new Label(string.Empty)
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill()
            };

            var statusLabel = new Label(string.Empty)
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill()
            };

            var listFrame = new FrameView("Secrets")
            {
                X = 0,
                Y = 3,
                Width = Dim.Percent(38),
                Height = Dim.Fill()
            };

            using var listView = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                CanFocus = true
            };

            var detailsFrame = new FrameView("Details")
            {
                X = Pos.Right(listFrame),
                Y = 3,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            using var detailsView = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ReadOnly = true,
                WordWrap = true,
                CanFocus = false
            };

            listFrame.Add(listView);
            detailsFrame.Add(detailsView);
            window.Add(summaryLabel, statusLabel, listFrame, detailsFrame);
            top.Add(window);

            void RefreshUi(string statusMessage)
            {
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
                statusLabel.Text = statusMessage;
                detailsFrame.Title = selectedSecret is null
                    ? "Details"
                    : $"Details: {selectedSecret.Source.Value}";
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

            listView.SelectedItemChanged += _ =>
            {
                if (suppressSelectionRefresh)
                {
                    return;
                }

                RefreshUi(statusLabel.Text?.ToString() ?? string.Empty);
            };
            listView.OpenSelectedItem += _ => EditSecret();

            using var statusBar = new StatusBar([
                new StatusItem(Key.N, "~N~ New", () => AddSecret()),
                new StatusItem(Key.E, "~E~ Edit", () => EditSecret()),
                new StatusItem(Key.D, "~D~ Delete", () => DeleteSecret()),
                new StatusItem(Key.P, "~P~ Reveal", () => TogglePasswordVisibility()),
                new StatusItem(Key.R, "~R~ Master", () => ChangeMasterPassword()),
                new StatusItem(Key.Esc, "~Esc~ Exit", () => Application.RequestStop())
            ]);

            top.Add(statusBar);
            RefreshUi("N new  E edit  D delete  P reveal  R master password  Esc exit");

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

        return $"Secrets: {vault.Count}  Updated: {vault.UpdatedUtc:yyyy-MM-dd HH:mm:ss} UTC  Selected: {selectedText}";
    }

    private static string BuildDetails(SecretRecord? secret, bool revealPasswords)
    {
        if (secret is null)
        {
            return "No secrets stored yet.\n\nPress N to create the first record.";
        }

        var builder = new StringBuilder();
        _ = builder.AppendLine("Source:   " + secret.Source.Value);
        _ = builder.AppendLine("Login:    " + secret.Login.Value);
        _ = builder.AppendLine(
            "Password: " + (revealPasswords ? secret.Password.Value : BuildMaskedPassword(secret.Password.Value)));
        _ = builder.AppendLine("Notes:    " + (secret.Notes.HasValue ? secret.Notes.Value : "(none)"));
        _ = builder.AppendLine("Created:  " + secret.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + " UTC");
        _ = builder.AppendLine("Updated:  " + secret.UpdatedUtc.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + " UTC");
        _ = builder.AppendLine("Id:       " + secret.Id);

        return builder.ToString();
    }

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

        using var dialog = new Dialog(existingSecret is null ? "New secret" : "Edit secret", 80, 20);
        dialog.Add(
            new Label("Source or service")
            {
                X = 1,
                Y = 1
            },
            sourceField,
            new Label("Login")
            {
                X = 1,
                Y = 4
            },
            loginField,
            new Label("Password")
            {
                X = 1,
                Y = 7
            },
            passwordField,
            new Label("Notes")
            {
                X = 1,
                Y = 10
            },
            notesView);

        using var saveButton = new Button("Save")
        {
            X = 18,
            Y = 16
        };
        using var cancelButton = new Button("Cancel")
        {
            X = 32,
            Y = 16
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

        using var dialog = new Dialog("Change master password", 80, 14);
        dialog.Add(
            new Label("New master password")
            {
                X = 1,
                Y = 1
            },
            passwordField,
            new Label("Confirm master password")
            {
                X = 1,
                Y = 4
            },
            confirmationField,
            new Label("16+ chars, uppercase, lowercase, digit, symbol, no whitespace")
            {
                X = 1,
                Y = 8,
                Width = Dim.Fill(2)
            });

        using var saveButton = new Button("Save")
        {
            X = 18,
            Y = 10
        };
        using var cancelButton = new Button("Cancel")
        {
            X = 32,
            Y = 10
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

    private readonly IClock _clock;
    private readonly ISecretVaultStore _vaultStore;

    private readonly record struct SecretDialogResult(
        string Source,
        string Login,
        string Password,
        string Notes);
}
