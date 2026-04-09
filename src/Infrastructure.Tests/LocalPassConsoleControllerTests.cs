using Abstractions;

using FluentAssertions;

using Models;

using Moq;

namespace Infrastructure.Tests;

public sealed class LocalPassConsoleControllerTests
{
    [Fact(DisplayName = "BuildScreenState should clamp selection to the last available password")]
    [Trait("Category", "Unit")]
    public void BuildScreenStateShouldClampSelectionToTheLastAvailablePassword()
    {
        var session = BuildSessionWithState(
            [SecretRecord.Create("Alpha", "first", "Password123!", null, Timestamp)],
            [],
            "Ready.");
        var controller = CreateController(session.Object);
        controller.SetSelectedIndex(10);

        var state = controller.BuildScreenState();

        state.SelectedIndex.Should().Be(0);
        state.StatusText.Should().Be("[ READY ] Ready.");
        state.DetailsTitle.Should().Be("Payload :: Alpha");
        state.ActiveTab.Should().Be(LocalPassVaultTab.Passwords);
    }

    [Fact(DisplayName = "AddItem should refresh and preserve the preferred selection from the session")]
    [Trait("Category", "Unit")]
    public void AddItemShouldRefreshAndPreserveThePreferredSelectionFromTheSession()
    {
        var input = SecretEditorInput.Create("GitHub", "user@example.com", "Password123!", "primary");
        var createdRecord = input.ToRecord(Timestamp);
        var session = BuildSessionWithState([], [], "Ready.");
        _ = session
            .Setup(item => item.AddSecret(input))
            .Returns(new LocalPassOperationResult(
                new SecretVault([createdRecord], Timestamp, Timestamp),
                "Saved GitHub.",
                createdRecord.Id));
        var controller = CreateController(session.Object, showSecretDialog: _ => input);

        var result = controller.AddItem();

        result.ShouldRefresh.Should().BeTrue();
        result.PreferredSelectionId.Should().Be(createdRecord.Id);
        session.Verify(item => item.AddSecret(input), Times.Once);
    }

    [Fact(DisplayName = "DeleteItem should update the status when password deletion is cancelled")]
    [Trait("Category", "Unit")]
    public void DeleteItemShouldUpdateTheStatusWhenPasswordDeletionIsCancelled()
    {
        var record = SecretRecord.Create("GitHub", "user@example.com", "Password123!", null, Timestamp);
        var session = BuildSessionWithState([record], [], "Ready.");
        var controller = CreateController(session.Object, confirmDeleteSecret: _ => false);

        var result = controller.DeleteItem();
        var state = controller.BuildScreenState();

        result.ShouldRefresh.Should().BeTrue();
        state.StatusText.Should().Be("[ READY ] Delete cancelled.");
        session.Verify(item => item.DeleteSecret(It.IsAny<Guid>()), Times.Never);
    }

    [Fact(DisplayName = "TogglePasswordVisibility should refresh and update the password details text")]
    [Trait("Category", "Unit")]
    public void TogglePasswordVisibilityShouldRefreshAndUpdateThePasswordDetailsText()
    {
        var record = SecretRecord.Create("GitHub", "user@example.com", "Password123!", null, Timestamp);
        var session = BuildSessionWithState([record], [], "Ready.");
        var controller = CreateController(session.Object);

        var result = controller.TogglePasswordVisibility();
        var state = controller.BuildScreenState();

        result.ShouldRefresh.Should().BeTrue();
        state.StatusText.Should().Be("[ READY ] Passwords are visible.");
        state.DetailsText.Should().Contain("PASSWORD  Password123!");
    }

    [Fact(DisplayName = "ToggleActiveTab should switch to notes and render note details")]
    [Trait("Category", "Unit")]
    public void ToggleActiveTabShouldSwitchToNotesAndRenderNoteDetails()
    {
        var note = SecureNoteRecord.Create("Taxes", "2025 filing", "important", Timestamp);
        var session = BuildSessionWithState([], [note], "Ready.");
        var controller = CreateController(session.Object);

        var result = controller.ToggleActiveTab();
        var state = controller.BuildScreenState();

        result.ShouldRefresh.Should().BeTrue();
        state.ActiveTab.Should().Be(LocalPassVaultTab.Notes);
        state.DetailsTitle.Should().Be("Secure Note :: Taxes");
        state.DetailsText.Should().Contain("SUMMARY    2025 filing");
        state.DetailsText.Should().NotContain("important");
        state.StatusText.Should().Be("[ READY ] Switched to secure notes.");
    }

    [Fact(DisplayName = "TogglePasswordVisibility should reveal note content when notes tab is active")]
    [Trait("Category", "Unit")]
    public void TogglePasswordVisibilityShouldRevealNoteContentWhenNotesTabIsActive()
    {
        var note = SecureNoteRecord.Create("Taxes", "2025 filing", "important", Timestamp);
        var session = BuildSessionWithState([], [note], "Ready.");
        var controller = CreateController(session.Object);
        _ = controller.ToggleActiveTab();

        var result = controller.TogglePasswordVisibility();
        var state = controller.BuildScreenState();

        result.ShouldRefresh.Should().BeTrue();
        state.StatusText.Should().Be("[ READY ] Note content is visible.");
        state.DetailsText.Should().Contain("important");
    }

    [Fact(DisplayName = "Switching from revealed passwords to notes should keep note content hidden by default")]
    [Trait("Category", "Unit")]
    public void SwitchingFromRevealedPasswordsToNotesShouldKeepNoteContentHiddenByDefault()
    {
        var secret = SecretRecord.Create("GitHub", "user@example.com", "Password123!", null, Timestamp);
        var note = SecureNoteRecord.Create("Taxes", "2025 filing", "important", Timestamp);
        var session = BuildSessionWithState([secret], [note], "Ready.");
        var controller = CreateController(session.Object);

        _ = controller.TogglePasswordVisibility();
        _ = controller.ToggleActiveTab();
        var state = controller.BuildScreenState();

        state.ActiveTab.Should().Be(LocalPassVaultTab.Notes);
        state.DetailsText.Should().NotContain("important");
        state.DetailsText.Should().Contain("CONTENT    ********* (9 chars hidden)");
    }

    [Fact(DisplayName = "AddItem should use the note workflow when notes tab is active")]
    [Trait("Category", "Unit")]
    public void AddItemShouldUseTheNoteWorkflowWhenNotesTabIsActive()
    {
        var input = SecureNoteEditorInput.Create("Taxes", "2025 filing", "important");
        var createdNote = input.ToRecord(Timestamp);
        var session = BuildSessionWithState([], [], "Ready.");
        _ = session
            .Setup(item => item.AddNote(input))
            .Returns(new LocalPassOperationResult(
                new SecretVault([], [createdNote], Timestamp, Timestamp),
                "Saved note Taxes.",
                createdNote.Id,
                LocalPassVaultTab.Notes));
        var controller = CreateController(session.Object, showNoteDialog: _ => input);
        _ = controller.ToggleActiveTab();

        var result = controller.AddItem();
        var state = controller.BuildScreenState(result.PreferredSelectionId);

        result.ShouldRefresh.Should().BeTrue();
        result.PreferredSelectionId.Should().Be(createdNote.Id);
        state.ActiveTab.Should().Be(LocalPassVaultTab.Notes);
        session.Verify(item => item.AddNote(input), Times.Once);
    }

    [Fact(DisplayName = "OpenStorageDirectory should return an error result when the session throws")]
    [Trait("Category", "Unit")]
    public void OpenStorageDirectoryShouldReturnAnErrorResultWhenTheSessionThrows()
    {
        var session = BuildSessionWithState([], [], "Ready.");
        _ = session
            .Setup(item => item.OpenStorageDirectory())
            .Throws(new InvalidOperationException("Cannot open folder."));
        var controller = CreateController(session.Object);

        var result = controller.OpenStorageDirectory();
        var state = controller.BuildScreenState();

        result.ShouldRefresh.Should().BeTrue();
        result.ErrorDialogTitle.Should().Be("Open folder failed");
        result.ErrorDialogMessage.Should().Be("Cannot open folder.");
        state.StatusText.Should().Be("[ READY ] Open folder failed: Cannot open folder.");
    }

    [Fact(DisplayName = "GenerateAndCopyStrongPassword should refresh after copying a generated password")]
    [Trait("Category", "Unit")]
    public void GenerateAndCopyStrongPasswordShouldRefreshAfterCopyingAGeneratedPassword()
    {
        const string generatedPassword = "Str0ng!Generated?Pwd";
        var session = BuildSessionWithState([], [], "Ready.");
        string? clipboardValue = null;
        var controller = CreateController(
            session.Object,
            generateStrongPassword: () => generatedPassword,
            copyToClipboard: value =>
            {
                clipboardValue = value;
                return true;
            });

        var result = controller.GenerateAndCopyStrongPassword();
        var state = controller.BuildScreenState();

        result.ShouldRefresh.Should().BeTrue();
        result.ErrorDialogTitle.Should().BeNull();
        clipboardValue.Should().Be(generatedPassword);
        state.StatusText.Should().Be("[ READY ] Generated strong password and copied it to the clipboard.");
    }

    [Fact(DisplayName = "GenerateAndCopyStrongPassword should return an error when clipboard copy fails")]
    [Trait("Category", "Unit")]
    public void GenerateAndCopyStrongPasswordShouldReturnAnErrorWhenClipboardCopyFails()
    {
        var session = BuildSessionWithState([], [], "Ready.");
        var controller = CreateController(
            session.Object,
            generateStrongPassword: () => "Str0ng!Generated?Pwd",
            copyToClipboard: _ => false);

        var result = controller.GenerateAndCopyStrongPassword();
        var state = controller.BuildScreenState();

        result.ShouldRefresh.Should().BeTrue();
        result.ErrorDialogTitle.Should().Be("Clipboard copy failed");
        result.ErrorDialogMessage.Should().Be("The generated password could not be copied to the clipboard.");
        state.StatusText.Should().Be("[ READY ] Clipboard copy failed: The generated password could not be copied to the clipboard.");
    }

    private static LocalPassConsoleController CreateController(
        ILocalPassConsoleSession session,
        Func<SecretRecord?, SecretEditorInput?>? showSecretDialog = null,
        Func<SecureNoteRecord?, SecureNoteEditorInput?>? showNoteDialog = null,
        Func<SecretRecord, bool>? confirmDeleteSecret = null,
        Func<SecureNoteRecord, bool>? confirmDeleteNote = null,
        Func<string>? generateStrongPassword = null,
        Func<string, bool>? copyToClipboard = null)
        => new(
            session,
            showSecretDialog ?? (_ => null),
            showNoteDialog ?? (_ => null),
            () => null,
            confirmDeleteSecret ?? (_ => false),
            confirmDeleteNote ?? (_ => false),
            generateStrongPassword ?? (() => StrongPasswordGenerator.Generate()),
            copyToClipboard ?? (_ => true));

    private static Mock<ILocalPassConsoleSession> BuildSessionWithState(
        IReadOnlyList<SecretRecord> records,
        IReadOnlyList<SecureNoteRecord> notes,
        string statusMessage)
    {
        var vault = new SecretVault(records, notes, Timestamp, Timestamp);
        var session = new Mock<ILocalPassConsoleSession>(MockBehavior.Strict);
        _ = session.SetupGet(item => item.CurrentVault).Returns(vault);
        _ = session.SetupGet(item => item.CurrentStatusMessage).Returns(statusMessage);
        return session;
    }

    private static readonly DateTimeOffset Timestamp = new(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
}
