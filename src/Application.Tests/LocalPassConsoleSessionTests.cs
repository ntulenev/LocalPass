using Abstractions;

using FluentAssertions;

using LocalPass.Application;

using Models;

using Moq;

using Xunit;

namespace Application.Tests;

public sealed class LocalPassConsoleSessionTests
{
    [Fact(DisplayName = "AddSecret should create a record, persist the session, and update the current view state")]
    [Trait("Category", "Unit")]
    public void AddSecretShouldCreateARecordPersistTheSessionAndUpdateTheCurrentViewState()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var session = CreateSession(timestamp);
        var input = SecretEditorInput.Create("GitHub", "user@example.com", "Password123!", "primary");
        var clock = new StubClock(timestamp);
        var store = new Mock<ISecretVaultStore>(MockBehavior.Strict);
        var storageLocation = new Mock<ISecretVaultStorageLocation>(MockBehavior.Strict);
        var folderOpener = new Mock<IFolderOpener>(MockBehavior.Strict);
        SecretVaultSession? capturedSession = null;
        _ = store
            .Setup(item => item.Save(It.IsAny<SecretVaultSession>()))
            .Returns<SecretVaultSession>(value =>
            {
                capturedSession = value;
                return value;
            });
        var appSession = new LocalPassConsoleSession(
            session,
            store.Object,
            clock,
            storageLocation.Object,
            folderOpener.Object);

        var result = appSession.AddSecret(input);

        capturedSession.Should().NotBeNull();
        capturedSession!.Vault.Count.Should().Be(1);
        appSession.CurrentVault.Count.Should().Be(1);
        appSession.CurrentStatusMessage.Should().Be("Saved GitHub.");
        result.CurrentState.Vault.Should().BeSameAs(capturedSession.Vault);
        result.CurrentState.StatusMessage.Should().Be("Saved GitHub.");
        result.PreferredSelectionId.Should().Be(capturedSession.Vault.GetSecret(0).Id);
        store.Verify(item => item.Save(It.IsAny<SecretVaultSession>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "EditSecret should update the selected record and keep its identifier selected")]
    [Trait("Category", "Unit")]
    public void EditSecretShouldUpdateTheSelectedRecordAndKeepItsIdentifierSelected()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var updatedTimestamp = timestamp.AddMinutes(30);
        var existingRecord = SecretRecord.Create("GitHub", "user@example.com", "Password123!", null, timestamp);
        var session = CreateSession(timestamp, existingRecord);
        var input = SecretEditorInput.Create("GitHub", "admin@example.com", "NewPassword123!", "rotated");
        var clock = new StubClock(updatedTimestamp);
        var store = new Mock<ISecretVaultStore>(MockBehavior.Strict);
        var storageLocation = new Mock<ISecretVaultStorageLocation>(MockBehavior.Strict);
        var folderOpener = new Mock<IFolderOpener>(MockBehavior.Strict);
        SecretVaultSession? capturedSession = null;
        _ = store
            .Setup(item => item.Save(It.IsAny<SecretVaultSession>()))
            .Returns<SecretVaultSession>(value =>
            {
                capturedSession = value;
                return value;
            });
        var appSession = new LocalPassConsoleSession(
            session,
            store.Object,
            clock,
            storageLocation.Object,
            folderOpener.Object);

        var result = appSession.EditSecret(existingRecord.Id, input);

        capturedSession.Should().NotBeNull();
        var updatedRecord = capturedSession!.Vault.GetSecret(0);
        updatedRecord.Id.Should().Be(existingRecord.Id);
        updatedRecord.Login.Value.Should().Be("admin@example.com");
        updatedRecord.Password.Value.Should().Be("NewPassword123!");
        updatedRecord.Notes.Value.Should().Be("rotated");
        updatedRecord.UpdatedUtc.Should().Be(updatedTimestamp);
        appSession.CurrentStatusMessage.Should().Be("Updated GitHub.");
        result.CurrentState.StatusMessage.Should().Be("Updated GitHub.");
        result.PreferredSelectionId.Should().Be(existingRecord.Id);
        store.Verify(item => item.Save(It.IsAny<SecretVaultSession>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "DeleteSecret should remove the record and persist the updated vault")]
    [Trait("Category", "Unit")]
    public void DeleteSecretShouldRemoveTheRecordAndPersistTheUpdatedVault()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var existingRecord = SecretRecord.Create("GitHub", "user@example.com", "Password123!", null, timestamp);
        var session = CreateSession(timestamp, existingRecord);
        var clock = new StubClock(timestamp.AddMinutes(1));
        var store = new Mock<ISecretVaultStore>(MockBehavior.Strict);
        var storageLocation = new Mock<ISecretVaultStorageLocation>(MockBehavior.Strict);
        var folderOpener = new Mock<IFolderOpener>(MockBehavior.Strict);
        SecretVaultSession? capturedSession = null;
        _ = store
            .Setup(item => item.Save(It.IsAny<SecretVaultSession>()))
            .Returns<SecretVaultSession>(value =>
            {
                capturedSession = value;
                return value;
            });
        var appSession = new LocalPassConsoleSession(
            session,
            store.Object,
            clock,
            storageLocation.Object,
            folderOpener.Object);

        var result = appSession.DeleteSecret(existingRecord.Id);

        capturedSession.Should().NotBeNull();
        capturedSession!.Vault.HasEntries.Should().BeFalse();
        appSession.CurrentStatusMessage.Should().Be("Secret deleted.");
        result.CurrentState.StatusMessage.Should().Be("Secret deleted.");
        result.PreferredSelectionId.Should().BeNull();
        store.Verify(item => item.Save(It.IsAny<SecretVaultSession>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "AddNote should create a secure note and switch the preferred tab to notes")]
    [Trait("Category", "Unit")]
    public void AddNoteShouldCreateASecureNoteAndSwitchThePreferredTabToNotes()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var session = CreateSession(timestamp);
        var input = SecureNoteEditorInput.Create("Taxes", "2025 filing", "important");
        var clock = new StubClock(timestamp);
        var store = new Mock<ISecretVaultStore>(MockBehavior.Strict);
        var storageLocation = new Mock<ISecretVaultStorageLocation>(MockBehavior.Strict);
        var folderOpener = new Mock<IFolderOpener>(MockBehavior.Strict);
        SecretVaultSession? capturedSession = null;
        _ = store
            .Setup(item => item.Save(It.IsAny<SecretVaultSession>()))
            .Returns<SecretVaultSession>(value =>
            {
                capturedSession = value;
                return value;
            });
        var appSession = new LocalPassConsoleSession(
            session,
            store.Object,
            clock,
            storageLocation.Object,
            folderOpener.Object);

        var result = appSession.AddNote(input);

        capturedSession.Should().NotBeNull();
        capturedSession!.Vault.NoteCount.Should().Be(1);
        appSession.CurrentVault.NoteCount.Should().Be(1);
        appSession.CurrentStatusMessage.Should().Be("Saved note Taxes.");
        result.PreferredTab.Should().Be(LocalPassVaultTab.Notes);
        result.PreferredSelectionId.Should().Be(capturedSession.Vault.GetNote(0).Id);
        store.Verify(item => item.Save(It.IsAny<SecretVaultSession>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "DeleteNote should remove the note and keep the notes tab active")]
    [Trait("Category", "Unit")]
    public void DeleteNoteShouldRemoveTheNoteAndKeepTheNotesTabActive()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var existingNote = SecureNoteRecord.Create("Taxes", "2025 filing", "important", timestamp);
        var session = CreateSession(timestamp, [], [existingNote]);
        var clock = new StubClock(timestamp.AddMinutes(1));
        var store = new Mock<ISecretVaultStore>(MockBehavior.Strict);
        var storageLocation = new Mock<ISecretVaultStorageLocation>(MockBehavior.Strict);
        var folderOpener = new Mock<IFolderOpener>(MockBehavior.Strict);
        SecretVaultSession? capturedSession = null;
        _ = store
            .Setup(item => item.Save(It.IsAny<SecretVaultSession>()))
            .Returns<SecretVaultSession>(value =>
            {
                capturedSession = value;
                return value;
            });
        var appSession = new LocalPassConsoleSession(
            session,
            store.Object,
            clock,
            storageLocation.Object,
            folderOpener.Object);

        var result = appSession.DeleteNote(existingNote.Id);

        capturedSession.Should().NotBeNull();
        capturedSession!.Vault.HasNotes.Should().BeFalse();
        appSession.CurrentStatusMessage.Should().Be("Secure note deleted.");
        result.PreferredTab.Should().Be(LocalPassVaultTab.Notes);
        store.Verify(item => item.Save(It.IsAny<SecretVaultSession>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "ChangeMasterPassword should delegate re-encryption to the vault store")]
    [Trait("Category", "Unit")]
    public void ChangeMasterPasswordShouldDelegateReEncryptionToTheVaultStore()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var session = CreateSession(timestamp);
        var newMasterPassword = new MasterPassword("AnotherStrongPassword1!");
        var updatedSession = session.WithMasterPassword(newMasterPassword);
        var clock = new StubClock(timestamp);
        var store = new Mock<ISecretVaultStore>(MockBehavior.Strict);
        var storageLocation = new Mock<ISecretVaultStorageLocation>(MockBehavior.Strict);
        var folderOpener = new Mock<IFolderOpener>(MockBehavior.Strict);
        _ = store
            .Setup(item => item.ChangeMasterPassword(session, newMasterPassword))
            .Returns(updatedSession);
        var appSession = new LocalPassConsoleSession(
            session,
            store.Object,
            clock,
            storageLocation.Object,
            folderOpener.Object);

        var result = appSession.ChangeMasterPassword(newMasterPassword);

        appSession.CurrentVault.Should().BeSameAs(updatedSession.Vault);
        appSession.CurrentStatusMessage.Should().Be("Master password updated and vault re-encrypted.");
        result.CurrentState.StatusMessage.Should().Be("Master password updated and vault re-encrypted.");
        result.PreferredSelectionId.Should().BeNull();
        store.Verify(item => item.ChangeMasterPassword(session, newMasterPassword), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "OpenStorageDirectory should open the reported storage path")]
    [Trait("Category", "Unit")]
    public void OpenStorageDirectoryShouldOpenTheReportedStoragePath()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var storagePath = Path.Combine(Path.GetTempPath(), "LocalPass.Storage");
        var clock = new StubClock(timestamp);
        var store = new Mock<ISecretVaultStore>(MockBehavior.Strict);
        var storageLocation = new Mock<ISecretVaultStorageLocation>(MockBehavior.Strict);
        var folderOpener = new Mock<IFolderOpener>(MockBehavior.Strict);
        _ = storageLocation
            .Setup(item => item.GetStorageDirectoryPath())
            .Returns(storagePath);
        folderOpener
            .Setup(item => item.OpenDirectory(storagePath));
        var appSession = new LocalPassConsoleSession(
            CreateSession(timestamp),
            store.Object,
            clock,
            storageLocation.Object,
            folderOpener.Object);

        var result = appSession.OpenStorageDirectory();

        result.Should().Be($"Opened storage directory: {storagePath}");
        appSession.CurrentStatusMessage.Should().Be(result);
        storageLocation.Verify(item => item.GetStorageDirectoryPath(), Times.Once);
        folderOpener.Verify(item => item.OpenDirectory(storagePath), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "Factory should create a session for unlocked vault state")]
    [Trait("Category", "Unit")]
    public void FactoryShouldCreateASessionForUnlockedVaultState()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var session = CreateSession(timestamp);
        var store = new Mock<ISecretVaultStore>(MockBehavior.Strict);
        var clock = new StubClock(timestamp);
        var storageLocation = new Mock<ISecretVaultStorageLocation>(MockBehavior.Strict);
        var folderOpener = new Mock<IFolderOpener>(MockBehavior.Strict);
        var factory = new LocalPassConsoleSessionFactory(
            store.Object,
            clock,
            storageLocation.Object,
            folderOpener.Object);

        var result = factory.Create(session);

        result.Should().BeOfType<LocalPassConsoleSession>();
    }

    private static SecretVaultSession CreateSession(DateTimeOffset timestamp, params SecretRecord[] entries)
        => CreateSession(timestamp, entries, []);

    private static SecretVaultSession CreateSession(
        DateTimeOffset timestamp,
        SecretRecord[] entries,
        SecureNoteRecord[] notes)
        => new(
            new SecretVault(entries, notes, timestamp, timestamp),
            new MasterPassword("StrongMasterPassword1!"));

    private sealed class StubClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
