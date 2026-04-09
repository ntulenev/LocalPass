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
    private readonly Func<SecretRecord, bool> _confirmDeleteSecret;
    private readonly Func<SecureNoteRecord, bool> _confirmDeleteNote;
    private readonly Func<SecretRecord?, SecretEditorInput?> _showSecretDialog;
    private readonly Func<SecureNoteRecord?, SecureNoteEditorInput?> _showNoteDialog;
    private readonly ILocalPassConsoleSession _session;

    /// <summary>
    /// Initializes a new console controller.
    /// </summary>
    /// <param name="session">Application session used by the console UI.</param>
    /// <param name="showSecretDialog">Dialog callback for creating or editing secrets.</param>
    /// <param name="showNoteDialog">Dialog callback for creating or editing secure notes.</param>
    /// <param name="showMasterPasswordDialog">Dialog callback for changing the master password.</param>
    /// <param name="confirmDeleteSecret">Confirmation callback used before deleting a secret.</param>
    /// <param name="confirmDeleteNote">Confirmation callback used before deleting a secure note.</param>
    /// <param name="generateStrongPassword">Strong password generator used by UI commands.</param>
    /// <param name="copyToClipboard">Clipboard writer used by UI commands.</param>
    public LocalPassConsoleController(
        ILocalPassConsoleSession session,
        Func<SecretRecord?, SecretEditorInput?> showSecretDialog,
        Func<SecureNoteRecord?, SecureNoteEditorInput?> showNoteDialog,
        Func<MasterPassword?> showMasterPasswordDialog,
        Func<SecretRecord, bool> confirmDeleteSecret,
        Func<SecureNoteRecord, bool> confirmDeleteNote,
        Func<string> generateStrongPassword,
        Func<string, bool> copyToClipboard)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _showSecretDialog = showSecretDialog ?? throw new ArgumentNullException(nameof(showSecretDialog));
        _showNoteDialog = showNoteDialog ?? throw new ArgumentNullException(nameof(showNoteDialog));
        _showMasterPasswordDialog = showMasterPasswordDialog
            ?? throw new ArgumentNullException(nameof(showMasterPasswordDialog));
        _confirmDeleteSecret = confirmDeleteSecret
            ?? throw new ArgumentNullException(nameof(confirmDeleteSecret));
        _confirmDeleteNote = confirmDeleteNote ?? throw new ArgumentNullException(nameof(confirmDeleteNote));
        _generateStrongPassword = generateStrongPassword
            ?? throw new ArgumentNullException(nameof(generateStrongPassword));
        _copyToClipboard = copyToClipboard ?? throw new ArgumentNullException(nameof(copyToClipboard));
        _currentStatusMessage = session.CurrentStatusMessage;
    }

    /// <summary>
    /// Builds the current screen state for the active tab.
    /// </summary>
    /// <param name="preferredSelectionId">Optional preferred selection to restore.</param>
    /// <param name="preferredTab">Optional preferred tab to activate.</param>
    /// <returns>The screen state snapshot.</returns>
    public LocalPassConsoleScreenState BuildScreenState(
        Guid? preferredSelectionId = null,
        LocalPassVaultTab? preferredTab = null)
    {
        if (preferredTab.HasValue)
        {
            _activeTab = preferredTab.Value;
        }

        var vault = _session.CurrentVault;
        var items = _activeTab == LocalPassVaultTab.Passwords
            ? LocalPassViewFormatter.BuildPasswordListItems(vault.Entries)
            : LocalPassViewFormatter.BuildNoteListItems(vault.Notes);
        var selectedIndex = ResolveSelectionIndex(items.Count, preferredSelectionId);
        SetSelectedIndex(selectedIndex);

        var selectedSecret = _activeTab == LocalPassVaultTab.Passwords ? GetSelectedSecret(selectedIndex) : null;
        var selectedNote = _activeTab == LocalPassVaultTab.Notes ? GetSelectedNote(selectedIndex) : null;
        var selectedLabel = selectedSecret is not null
            ? $"{selectedSecret.Source.Value} / {selectedSecret.Login.Value}"
            : selectedNote?.Title.Value;

        return new LocalPassConsoleScreenState(
            items,
            selectedIndex,
            _activeTab,
            LocalPassViewFormatter.BuildSummary(vault, _activeTab, selectedLabel),
            LocalPassViewFormatter.FormatStatus(_currentStatusMessage),
            LocalPassViewFormatter.BuildIndexTitle(_activeTab),
            BuildDetailsTitle(selectedSecret, selectedNote),
            _activeTab == LocalPassVaultTab.Passwords
                ? LocalPassViewFormatter.BuildPasswordDetails(selectedSecret, _revealPasswords)
                : LocalPassViewFormatter.BuildNoteDetails(selectedNote));
    }

    /// <summary>
    /// Updates the selected index for the active tab.
    /// </summary>
    /// <param name="selectedIndex">Current list selection.</param>
    public void SetSelectedIndex(int selectedIndex)
    {
        if (_activeTab == LocalPassVaultTab.Passwords)
        {
            _passwordSelectionIndex = selectedIndex;
        }
        else
        {
            _noteSelectionIndex = selectedIndex;
        }
    }

    /// <summary>
    /// Switches the active tab.
    /// </summary>
    /// <returns>The command result.</returns>
    public LocalPassConsoleCommandResult ToggleActiveTab()
    {
        _activeTab = _activeTab == LocalPassVaultTab.Passwords
            ? LocalPassVaultTab.Notes
            : LocalPassVaultTab.Passwords;
        _currentStatusMessage = _activeTab == LocalPassVaultTab.Passwords
            ? "Switched to passwords."
            : "Switched to secure notes.";
        return LocalPassConsoleCommandResult.Refresh();
    }

    /// <summary>
    /// Handles creation of a new item in the active tab.
    /// </summary>
    /// <returns>The command result.</returns>
    public LocalPassConsoleCommandResult AddItem()
        => _activeTab == LocalPassVaultTab.Passwords ? AddSecret() : AddNote();

    /// <summary>
    /// Handles editing of the selected item in the active tab.
    /// </summary>
    /// <returns>The command result.</returns>
    public LocalPassConsoleCommandResult EditItem()
        => _activeTab == LocalPassVaultTab.Passwords
            ? EditSecret(_passwordSelectionIndex)
            : EditNote(_noteSelectionIndex);

    /// <summary>
    /// Handles deletion of the selected item in the active tab.
    /// </summary>
    /// <returns>The command result.</returns>
    public LocalPassConsoleCommandResult DeleteItem()
        => _activeTab == LocalPassVaultTab.Passwords
            ? DeleteSecret(_passwordSelectionIndex)
            : DeleteNote(_noteSelectionIndex);

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

    private LocalPassConsoleCommandResult AddSecret()
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

    private LocalPassConsoleCommandResult AddNote()
    {
        var input = _showNoteDialog(null);
        if (input is null)
        {
            return LocalPassConsoleCommandResult.NoChange();
        }

        try
        {
            return ApplyResult(_session.AddNote(input));
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or InvalidDataException)
        {
            return HandleError("Save failed", exception.Message);
        }
    }

    private LocalPassConsoleCommandResult EditSecret(int selectedIndex)
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

    private LocalPassConsoleCommandResult EditNote(int selectedIndex)
    {
        var selectedNote = GetSelectedNote(selectedIndex);
        if (selectedNote is null)
        {
            _currentStatusMessage = "No secure note selected.";
            return LocalPassConsoleCommandResult.Refresh();
        }

        var input = _showNoteDialog(selectedNote);
        if (input is null)
        {
            return LocalPassConsoleCommandResult.NoChange();
        }

        try
        {
            return ApplyResult(_session.EditNote(selectedNote.Id, input));
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or InvalidDataException)
        {
            return HandleError("Save failed", exception.Message);
        }
    }

    private LocalPassConsoleCommandResult DeleteSecret(int selectedIndex)
    {
        var selectedSecret = GetSelectedSecret(selectedIndex);
        if (selectedSecret is null)
        {
            _currentStatusMessage = "No secret selected.";
            return LocalPassConsoleCommandResult.Refresh();
        }

        if (!_confirmDeleteSecret(selectedSecret))
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

    private LocalPassConsoleCommandResult DeleteNote(int selectedIndex)
    {
        var selectedNote = GetSelectedNote(selectedIndex);
        if (selectedNote is null)
        {
            _currentStatusMessage = "No secure note selected.";
            return LocalPassConsoleCommandResult.Refresh();
        }

        if (!_confirmDeleteNote(selectedNote))
        {
            _currentStatusMessage = "Delete cancelled.";
            return LocalPassConsoleCommandResult.Refresh();
        }

        try
        {
            return ApplyResult(_session.DeleteNote(selectedNote.Id));
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or InvalidDataException)
        {
            return HandleError("Save failed", exception.Message);
        }
    }

    private LocalPassConsoleCommandResult ApplyResult(LocalPassOperationResult result)
    {
        _currentStatusMessage = result.CurrentState.StatusMessage;
        if (result.PreferredTab.HasValue)
        {
            _activeTab = result.PreferredTab.Value;
        }

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

    private SecureNoteRecord? GetSelectedNote(int selectedIndex)
        => !_session.CurrentVault.HasNotes || selectedIndex < 0 || selectedIndex >= _session.CurrentVault.NoteCount
            ? null
            : _session.CurrentVault.GetNote(selectedIndex);

    private int ResolveSelectionIndex(int itemCount, Guid? preferredSelectionId)
    {
        if (itemCount == 0)
        {
            return 0;
        }

        if (preferredSelectionId.HasValue)
        {
            var preferredIndex = _activeTab == LocalPassVaultTab.Passwords
                ? _session.CurrentVault.FindIndex(preferredSelectionId.Value)
                : _session.CurrentVault.FindNoteIndex(preferredSelectionId.Value);
            if (preferredIndex >= 0)
            {
                return preferredIndex;
            }
        }

        var currentSelection = _activeTab == LocalPassVaultTab.Passwords
            ? _passwordSelectionIndex
            : _noteSelectionIndex;
        return Math.Clamp(currentSelection, 0, itemCount - 1);
    }

    private string BuildDetailsTitle(SecretRecord? selectedSecret, SecureNoteRecord? selectedNote)
        => _activeTab == LocalPassVaultTab.Passwords
            ? selectedSecret is null ? "Payload Inspect" : $"Payload :: {selectedSecret.Source.Value}"
            : selectedNote is null ? "Secure Note Inspect" : $"Secure Note :: {selectedNote.Title.Value}";

    private LocalPassVaultTab _activeTab = LocalPassVaultTab.Passwords;
    private string _currentStatusMessage;
    private int _noteSelectionIndex;
    private int _passwordSelectionIndex;
    private bool _revealPasswords;
}
