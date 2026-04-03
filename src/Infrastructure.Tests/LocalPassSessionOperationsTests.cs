using Abstractions;

using FluentAssertions;

using Models;

using Moq;

namespace Infrastructure.Tests;

public sealed class LocalPassSessionOperationsTests
{
    [Fact(DisplayName = "AddSecret should create a record, persist the session, and prefer the new selection")]
    [Trait("Category", "Unit")]
    public void AddSecretShouldCreateARecordPersistTheSessionAndPreferTheNewSelection()
    {
        // Arrange
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
        var operations = new LocalPassSessionOperations(
            store.Object,
            clock,
            storageLocation.Object,
            folderOpener.Object);

        // Act
        var result = operations.AddSecret(session, input);

        // Assert
        capturedSession.Should().NotBeNull();
        capturedSession!.Vault.Count.Should().Be(1);
        capturedSession.Vault.GetSecret(0).Source.Value.Should().Be("GitHub");
        capturedSession.Vault.GetSecret(0).Login.Value.Should().Be("user@example.com");
        capturedSession.Vault.GetSecret(0).Password.Value.Should().Be("Password123!");
        capturedSession.Vault.GetSecret(0).Notes.Value.Should().Be("primary");
        result.Session.Should().BeSameAs(capturedSession);
        result.StatusMessage.Should().Be("Saved GitHub.");
        result.PreferredSelectionId.Should().Be(capturedSession.Vault.GetSecret(0).Id);
        store.Verify(item => item.Save(It.IsAny<SecretVaultSession>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "EditSecret should update the existing record and keep its identifier selected")]
    [Trait("Category", "Unit")]
    public void EditSecretShouldUpdateTheExistingRecordAndKeepItsIdentifierSelected()
    {
        // Arrange
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
        var operations = new LocalPassSessionOperations(
            store.Object,
            clock,
            storageLocation.Object,
            folderOpener.Object);

        // Act
        var result = operations.EditSecret(session, existingRecord, input);

        // Assert
        capturedSession.Should().NotBeNull();
        var updatedRecord = capturedSession!.Vault.GetSecret(0);
        updatedRecord.Id.Should().Be(existingRecord.Id);
        updatedRecord.Login.Value.Should().Be("admin@example.com");
        updatedRecord.Password.Value.Should().Be("NewPassword123!");
        updatedRecord.Notes.Value.Should().Be("rotated");
        updatedRecord.UpdatedUtc.Should().Be(updatedTimestamp);
        result.StatusMessage.Should().Be("Updated GitHub.");
        result.PreferredSelectionId.Should().Be(existingRecord.Id);
        store.Verify(item => item.Save(It.IsAny<SecretVaultSession>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "DeleteSecret should remove the record and persist the updated vault")]
    [Trait("Category", "Unit")]
    public void DeleteSecretShouldRemoveTheRecordAndPersistTheUpdatedVault()
    {
        // Arrange
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
        var operations = new LocalPassSessionOperations(
            store.Object,
            clock,
            storageLocation.Object,
            folderOpener.Object);

        // Act
        var result = operations.DeleteSecret(session, existingRecord);

        // Assert
        capturedSession.Should().NotBeNull();
        capturedSession!.Vault.HasEntries.Should().BeFalse();
        result.StatusMessage.Should().Be("Secret deleted.");
        result.PreferredSelectionId.Should().BeNull();
        store.Verify(item => item.Save(It.IsAny<SecretVaultSession>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "ChangeMasterPassword should delegate re-encryption to the vault store")]
    [Trait("Category", "Unit")]
    public void ChangeMasterPasswordShouldDelegateReEncryptionToTheVaultStore()
    {
        // Arrange
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
        var operations = new LocalPassSessionOperations(
            store.Object,
            clock,
            storageLocation.Object,
            folderOpener.Object);

        // Act
        var result = operations.ChangeMasterPassword(session, newMasterPassword);

        // Assert
        result.Session.Should().BeSameAs(updatedSession);
        result.StatusMessage.Should().Be("Master password updated and vault re-encrypted.");
        result.PreferredSelectionId.Should().BeNull();
        store.Verify(item => item.ChangeMasterPassword(session, newMasterPassword), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "OpenStorageDirectory should open the reported storage path")]
    [Trait("Category", "Unit")]
    public void OpenStorageDirectoryShouldOpenTheReportedStoragePath()
    {
        // Arrange
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
        var operations = new LocalPassSessionOperations(
            store.Object,
            clock,
            storageLocation.Object,
            folderOpener.Object);

        // Act
        var result = operations.OpenStorageDirectory();

        // Assert
        result.Should().Be($"Opened storage directory: {storagePath}");
        storageLocation.Verify(item => item.GetStorageDirectoryPath(), Times.Once);
        folderOpener.Verify(item => item.OpenDirectory(storagePath), Times.Once);
        store.VerifyNoOtherCalls();
    }

    private static SecretVaultSession CreateSession(DateTimeOffset timestamp, params SecretRecord[] entries)
        => new(new SecretVault(entries, timestamp, timestamp), new MasterPassword("StrongMasterPassword1!"));

    private sealed class StubClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
