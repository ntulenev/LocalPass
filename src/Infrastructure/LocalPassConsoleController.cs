using Abstractions;

using Models;

using System.ComponentModel;
using System.IO;

namespace Infrastructure;

/// <summary>
/// Coordinates UI actions and screen state for the LocalPass console renderer.
/// </summary>
public sealed class LocalPassConsoleController
{
    private readonly Func<string, bool> _copyToClipboard;
    private readonly Func<string> _generateStrongPassword;
    private readonly Func<MasterPassword?> _showMasterPasswordDialog;
    private readonly Func<SecretRecord, bool> _confirmDelete;
    private readonly Func<SecretRecord?, SecretEditorInput?> _showSecretDialog;
    private readonly ILocalPassConsoleSession _session;

    /// <summary>
    /// Initializes a new console controller.
    /// </summary>
    /// <param name="session">Application session used by the console UI.</param>
    /// <param name="showSecretDialog">Dialog callback for creating or editing secrets.</param>
    /// <param name="showMasterPasswordDialog">Dialog callback for changing the master password.</param>
    /// <param name="confirmDelete">Confirmation callback used before deleting a secret.</param>
    /// <param name="generateStrongPassword">Strong password generator used by UI commands.</param>
    /// <param name="copyToClipboard">Clipboard writer used by UI commands.</param>
    public LocalPassConsoleController(
        ILocalPassConsoleSession session,
        Func<SecretRecord?, SecretEditorInput?> showSecretDialog,
        Func<MasterPassword?> showMasterPasswordDialog,
        Func<SecretRecord, bool> confirmDelete,
        Func<string> generateStrongPassword,
        Func<string, bool> copyToClipboard)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _showSecretDialog = showSecretDialog ?? throw new ArgumentNullException(nameof(showSecretDialog));
        _showMasterPasswordDialog = showMasterPasswordDialog
            ?? throw new ArgumentNullException(nameof(showMasterPasswordDialog));
        _confirmDelete = confirmDelete ?? throw new ArgumentNullException(nameof(confirmDelete));
        _generateStrongPassword = generateStrongPassword
            ?? throw new ArgumentNullException(nameof(generateStrongPassword));
        _copyToClipboard = copyToClipboard ?? throw new ArgumentNullException(nameof(copyToClipboard));
        _currentStatusMessage = session.CurrentStatusMessage;
    }

    /// <summary>
    /// Builds the current screen state for the supplied selection context.
    /// </summary>
    /// <param name="previousSelection">Previously selected index.</param>
    /// <param name="preferredSelectionId">Optional preferred selection to restore.</param>
    /// <returns>The screen state snapshot.</returns>
    public LocalPassConsoleScreenState BuildScreenState(int previousSelection, Guid? preferredSelectionId = null)
    {
        var items = LocalPassViewFormatter.BuildListItems(_session.CurrentVault);
        var selectedIndex = ResolveSelectionIndex(items.Count, previousSelection, preferredSelectionId);
        var selectedSecret = GetSelectedSecret(selectedIndex);

        return new LocalPassConsoleScreenState(
            items,
            selectedIndex,
            LocalPassViewFormatter.BuildSummary(_session.CurrentVault, selectedSecret),
            LocalPassViewFormatter.FormatStatus(_currentStatusMessage),
            selectedSecret is null ? "Payload Inspect" : $"Payload :: {selectedSecret.Source.Value}",
            LocalPassViewFormatter.BuildDetails(selectedSecret, _revealPasswords));
    }

    /// <summary>
    /// Handles creation of a new secret.
    /// </summary>
    /// <returns>The command result.</returns>
    public LocalPassConsoleCommandResult AddSecret()
    {
        var input = _showSecretDialog(null);
        if (input is null)
        {
            return LocalPassConsoleCommandResult.NoChange();
        }

        try
        {
            return ApplyResult(_session.AddSecret(input));
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or InvalidDataException)
        {
            return HandleError("Save failed", exception.Message);
        }
    }

    /// <summary>
    /// Handles editing of the selected secret.
    /// </summary>
    /// <param name="selectedIndex">Currently selected index.</param>
    /// <returns>The command result.</returns>
    public LocalPassConsoleCommandResult EditSecret(int selectedIndex)
    {
        var selectedSecret = GetSelectedSecret(selectedIndex);
        if (selectedSecret is null)
        {
            _currentStatusMessage = "No secret selected.";
            return LocalPassConsoleCommandResult.Refresh();
        }

        var input = _showSecretDialog(selectedSecret);
        if (input is null)
        {
            return LocalPassConsoleCommandResult.NoChange();
        }

        try
        {
            return ApplyResult(_session.EditSecret(selectedSecret.Id, input));
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or InvalidDataException)
        {
            return HandleError("Save failed", exception.Message);
        }
    }

    /// <summary>
    /// Handles deletion of the selected secret.
    /// </summary>
    /// <param name="selectedIndex">Currently selected index.</param>
    /// <returns>The command result.</returns>
    public LocalPassConsoleCommandResult DeleteSecret(int selectedIndex)
    {
        var selectedSecret = GetSelectedSecret(selectedIndex);
        if (selectedSecret is null)
        {
            _currentStatusMessage = "No secret selected.";
            return LocalPassConsoleCommandResult.Refresh();
        }

        if (!_confirmDelete(selectedSecret))
        {
            _currentStatusMessage = "Delete cancelled.";
            return LocalPassConsoleCommandResult.Refresh();
        }

        try
        {
            return ApplyResult(_session.DeleteSecret(selectedSecret.Id));
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or InvalidDataException)
        {
            return HandleError("Save failed", exception.Message);
        }
    }

    /// <summary>
    /// Toggles password visibility in the details pane.
    /// </summary>
    /// <returns>The command result.</returns>
    public LocalPassConsoleCommandResult TogglePasswordVisibility()
    {
        _revealPasswords = !_revealPasswords;
        _currentStatusMessage = _revealPasswords ? "Passwords are visible." : "Passwords are hidden.";
        return LocalPassConsoleCommandResult.Refresh();
    }

    /// <summary>
    /// Handles master password rotation.
    /// </summary>
    /// <returns>The command result.</returns>
    public LocalPassConsoleCommandResult ChangeMasterPassword()
    {
        var newMasterPassword = _showMasterPasswordDialog();
        if (newMasterPassword is null)
        {
            return LocalPassConsoleCommandResult.NoChange();
        }

        try
        {
            return ApplyResult(_session.ChangeMasterPassword(newMasterPassword));
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or InvalidDataException)
        {
            return HandleError("Password change failed", exception.Message);
        }
    }

    /// <summary>
    /// Generates a strong password and copies it to the clipboard.
    /// </summary>
    /// <returns>The command result.</returns>
    public LocalPassConsoleCommandResult GenerateAndCopyStrongPassword()
    {
        var generatedPassword = _generateStrongPassword();
        if (string.IsNullOrWhiteSpace(generatedPassword))
        {
            return HandleError("Password generation failed", "Generated password was empty.");
        }

        if (!_copyToClipboard(generatedPassword))
        {
            return HandleError("Clipboard copy failed", "The generated password could not be copied to the clipboard.");
        }

        _currentStatusMessage = "Generated strong password and copied it to the clipboard.";
        return LocalPassConsoleCommandResult.Refresh();
    }

    /// <summary>
    /// Handles opening of the storage directory.
    /// </summary>
    /// <returns>The command result.</returns>
    public LocalPassConsoleCommandResult OpenStorageDirectory()
    {
        try
        {
            _currentStatusMessage = _session.OpenStorageDirectory();
            return LocalPassConsoleCommandResult.Refresh();
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or DirectoryNotFoundException
            or InvalidOperationException
            or Win32Exception)
        {
            return HandleError("Open folder failed", exception.Message);
        }
    }

    private LocalPassConsoleCommandResult ApplyResult(LocalPassOperationResult result)
    {
        _currentStatusMessage = result.CurrentState.StatusMessage;
        return LocalPassConsoleCommandResult.Refresh(result.PreferredSelectionId);
    }

    private LocalPassConsoleCommandResult HandleError(string title, string message)
    {
        _currentStatusMessage = $"{title}: {message}";
        return LocalPassConsoleCommandResult.Error(title, message);
    }

    private SecretRecord? GetSelectedSecret(int selectedIndex)
        => !_session.CurrentVault.HasEntries || selectedIndex < 0 || selectedIndex >= _session.CurrentVault.Count
            ? null
            : _session.CurrentVault.GetSecret(selectedIndex);

    private int ResolveSelectionIndex(int itemCount, int previousSelection, Guid? preferredSelectionId)
    {
        if (itemCount == 0)
        {
            return 0;
        }

        if (preferredSelectionId.HasValue)
        {
            var preferredIndex = _session.CurrentVault.FindIndex(preferredSelectionId.Value);
            if (preferredIndex >= 0)
            {
                return preferredIndex;
            }
        }

        return Math.Clamp(previousSelection, 0, itemCount - 1);
    }

    private string _currentStatusMessage;
    private bool _revealPasswords;
}
