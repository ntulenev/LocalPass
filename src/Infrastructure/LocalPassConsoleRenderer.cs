using Abstractions;

using Infrastructure.Dialogs;

using Models;

using Terminal.Gui;

namespace Infrastructure;

/// <summary>
/// Renders the interactive LocalPass UI by using Terminal.Gui.
/// </summary>
public sealed class LocalPassConsoleRenderer : ISecretVaultConsoleRenderer
{
    /// <inheritdoc />
    public Task RunAsync(ILocalPassConsoleSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        var suppressSelectionRefresh = false;

        Application.Init();

        try
        {
            var top = Application.Top;
            var chromeScheme = LocalPassConsoleTheme.CreateChromeScheme();
            var accentScheme = LocalPassConsoleTheme.CreateAccentScheme();
            var focusScheme = LocalPassConsoleTheme.CreateFocusScheme();
            top.ColorScheme = chromeScheme;

            using var window = LocalPassConsoleLayout.CreateWindow(chromeScheme);
            var summaryLabel = LocalPassConsoleLayout.CreateSummaryLabel(accentScheme);
            var statusLabel = LocalPassConsoleLayout.CreateStatusLabel(focusScheme);
            var listFrame = LocalPassConsoleLayout.CreateSecretIndexFrame(chromeScheme);
            using var listView = LocalPassConsoleLayout.CreateSecretListView(focusScheme);
            var detailsFrame = LocalPassConsoleLayout.CreatePayloadInspectFrame(chromeScheme, listFrame);
            using var detailsView = LocalPassConsoleLayout.CreatePayloadDetailsView(accentScheme);

            listFrame.Add(listView);
            detailsFrame.Add(detailsView);
            window.Add(summaryLabel, statusLabel, listFrame, detailsFrame);
            top.Add(window);
            var controller = new LocalPassConsoleController(
                session,
                SecretEditorDialog.Prompt,
                MasterPasswordDialog.Prompt,
                ConfirmDelete);

            void RefreshUi(Guid? preferredSelectionId = null)
            {
                var screenState = controller.BuildScreenState(listView.SelectedItem, preferredSelectionId);

                listView.SetSource(screenState.Items.ToList());
                if (listView.SelectedItem != screenState.SelectedIndex)
                {
                    suppressSelectionRefresh = true;
                    try
                    {
                        listView.SelectedItem = screenState.SelectedIndex;
                    }
                    finally
                    {
                        suppressSelectionRefresh = false;
                    }
                }

                summaryLabel.Text = screenState.SummaryText;
                statusLabel.Text = screenState.StatusText;
                detailsFrame.Title = screenState.DetailsTitle;
                detailsView.Text = screenState.DetailsText;
            }

            void ApplyCommand(LocalPassConsoleCommandResult result)
            {
                if (!string.IsNullOrWhiteSpace(result.ErrorDialogTitle) &&
                    !string.IsNullOrWhiteSpace(result.ErrorDialogMessage))
                {
                    MessageBox.ErrorQuery(result.ErrorDialogTitle, result.ErrorDialogMessage, "OK");
                }

                if (result.ShouldRefresh)
                {
                    RefreshUi(result.PreferredSelectionId);
                }
            }

            listView.SelectedItemChanged += _ =>
            {
                if (suppressSelectionRefresh)
                {
                    return;
                }

                RefreshUi();
            };
            listView.OpenSelectedItem += _ => ApplyCommand(controller.EditSecret(listView.SelectedItem));

            using var statusBar = new StatusBar([
                new StatusItem(Key.N, "~N~ New", () => ApplyCommand(controller.AddSecret())),
                new StatusItem(Key.E, "~E~ Edit", () => ApplyCommand(controller.EditSecret(listView.SelectedItem))),
                new StatusItem(Key.D, "~D~ Delete", () => ApplyCommand(controller.DeleteSecret(listView.SelectedItem))),
                new StatusItem(Key.O, "~O~ Files", () => ApplyCommand(controller.OpenStorageDirectory())),
                new StatusItem(Key.P, "~P~ Reveal", () => ApplyCommand(controller.TogglePasswordVisibility())),
                new StatusItem(Key.R, "~R~ Master", () => ApplyCommand(controller.ChangeMasterPassword())),
                new StatusItem(Key.Esc, "~Esc~ Exit", () => Application.RequestStop())
            ])
            {
                ColorScheme = chromeScheme
            };

            top.Add(statusBar);
            RefreshUi();

            using var registration = cancellationToken.Register(() => Application.RequestStop());
            Application.Run();
        }
        finally
        {
            Application.Shutdown();
        }

        return Task.CompletedTask;
    }

    private static bool ConfirmDelete(SecretRecord secret)
        => MessageBox.Query(
            "Delete secret",
            $"Delete {secret.Source.Value} / {secret.Login.Value}?",
            "Delete",
            "Cancel") == 0;
}
