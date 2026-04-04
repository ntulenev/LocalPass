using Abstractions;

using FluentAssertions;

using Models;

namespace Infrastructure.Tests;

public sealed class FileSecretVaultStoreTests : IDisposable
{
    private readonly string _storageDirectory = Path.Combine(
        Path.GetTempPath(),
        "LocalPass.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact(DisplayName = "CreateNew and Open should round-trip encrypted vault contents")]
    [Trait("Category", "Unit")]
    public void CreateNewAndOpenShouldRoundTripEncryptedVaultContents()
    {
        // Arrange
        var clock = new StubClock(new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero));
        var store = new FileSecretVaultStore(_storageDirectory, clock);
        var masterPassword = new MasterPassword("StrongMasterPassword1!");
        var createdSession = store.CreateNew(masterPassword);
        var record = SecretRecord.Create(
            "GitHub",
            "user@example.com",
            "Password123!",
            "primary account",
            clock.UtcNow);

        // Act
        _ = store.Save(createdSession.WithVault(createdSession.Vault.WithEntry(record, clock.UtcNow)));
        var reopenedSession = store.Open(masterPassword);

        // Assert
        reopenedSession.Vault.Count.Should().Be(1);
        reopenedSession.Vault.GetSecret(0).Source.Value.Should().Be("GitHub");
        reopenedSession.Vault.GetSecret(0).Login.Value.Should().Be("user@example.com");
        reopenedSession.Vault.GetSecret(0).Password.Value.Should().Be("Password123!");
        reopenedSession.Vault.DocumentVersion.Should().Be(2);
    }

    [Fact(DisplayName = "Save should create a snapshot when replacing an existing vault file")]
    [Trait("Category", "Unit")]
    public void SaveShouldCreateASnapshotWhenReplacingAnExistingVaultFile()
    {
        // Arrange
        var clock = new StubClock(new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero));
        var store = new FileSecretVaultStore(_storageDirectory, clock);
        var masterPassword = new MasterPassword("StrongMasterPassword1!");
        var session = store.CreateNew(masterPassword);
        var firstRecord = SecretRecord.Create("GitHub", "user1", "Password123!", null, clock.UtcNow);
        session = store.Save(session.WithVault(session.Vault.WithEntry(firstRecord, clock.UtcNow)));

        clock.Set(new DateTimeOffset(2026, 4, 3, 12, 30, 0, TimeSpan.Zero));
        var secondRecord = SecretRecord.Create("GitLab", "user2", "Password123!", null, clock.UtcNow);

        // Act
        _ = store.Save(session.WithVault(session.Vault.WithEntry(secondRecord, clock.UtcNow)));

        // Assert
        var snapshotDirectory = Path.Combine(_storageDirectory, "snapshots");
        Directory.GetFiles(snapshotDirectory, "*.localpass").Should().HaveCount(2);
    }

    [Fact(DisplayName = "Open should throw when master password is incorrect")]
    [Trait("Category", "Unit")]
    public void OpenShouldThrowWhenMasterPasswordIsIncorrect()
    {
        // Arrange
        var clock = new StubClock(new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero));
        var store = new FileSecretVaultStore(_storageDirectory, clock);
        _ = store.CreateNew(new MasterPassword("StrongMasterPassword1!"));

        // Act
        var action = () => store.Open(new MasterPassword("AnotherMasterPassword1!"));

        // Assert
        var exception = action.Should().Throw<InvalidDataException>().Which;
        exception.Message.Should().Be("The provided master password is incorrect or the vault file is corrupted.");
    }

    [Fact(DisplayName = "Open should throw when the vault file does not exist")]
    [Trait("Category", "Unit")]
    public void OpenShouldThrowWhenTheVaultFileDoesNotExist()
    {
        // Arrange
        var clock = new StubClock(new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero));
        var store = new FileSecretVaultStore(_storageDirectory, clock);

        // Act
        var action = () => store.Open(new MasterPassword("StrongMasterPassword1!"));

        // Assert
        var exception = action.Should().Throw<FileNotFoundException>().Which;
        exception.Message.Should().Contain("Vault file was not found.");
    }

    [Fact(DisplayName = "Open should throw when the vault file contains invalid JSON")]
    [Trait("Category", "Unit")]
    public void OpenShouldThrowWhenTheVaultFileContainsInvalidJson()
    {
        // Arrange
        var clock = new StubClock(new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero));
        var store = new FileSecretVaultStore(_storageDirectory, clock);
        Directory.CreateDirectory(_storageDirectory);
        File.WriteAllText(
            Path.Combine(_storageDirectory, "vault.localpass"),
            "{ invalid json",
            System.Text.Encoding.UTF8);

        // Act
        var action = () => store.Open(new MasterPassword("StrongMasterPassword1!"));

        // Assert
        var exception = action.Should().Throw<InvalidDataException>().Which;
        exception.Message.Should().Be("Vault file is invalid.");
    }

    [Fact(DisplayName = "ChangeMasterPassword should re-encrypt the vault for the new password")]
    [Trait("Category", "Unit")]
    public void ChangeMasterPasswordShouldReEncryptTheVaultForTheNewPassword()
    {
        // Arrange
        var clock = new StubClock(new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero));
        var store = new FileSecretVaultStore(_storageDirectory, clock);
        var originalPassword = new MasterPassword("StrongMasterPassword1!");
        var updatedPassword = new MasterPassword("AnotherStrongPassword1!");
        var session = store.CreateNew(originalPassword);
        var record = SecretRecord.Create(
            "GitHub",
            "user@example.com",
            "Password123!",
            "primary account",
            clock.UtcNow);
        session = store.Save(session.WithVault(session.Vault.WithEntry(record, clock.UtcNow)));

        // Act
        _ = store.ChangeMasterPassword(session, updatedPassword);
        var reopenedSession = store.Open(updatedPassword);
        var oldPasswordAction = () => store.Open(originalPassword);

        // Assert
        reopenedSession.Vault.Count.Should().Be(1);
        reopenedSession.Vault.GetSecret(0).Source.Value.Should().Be("GitHub");
        oldPasswordAction.Should().Throw<InvalidDataException>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_storageDirectory))
        {
            Directory.Delete(_storageDirectory, recursive: true);
        }
    }

    private sealed class StubClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => _utcNow;

        public void Set(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        private DateTimeOffset _utcNow = utcNow;
    }
}
