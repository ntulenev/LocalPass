using FluentAssertions;

using Models;

namespace Infrastructure.Tests;

public sealed class VaultCryptographyServiceTests
{
    [Fact(DisplayName = "Encrypt and decrypt should round-trip the vault contents")]
    [Trait("Category", "Unit")]
    public void EncryptAndDecryptShouldRoundTripTheVaultContents()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var masterPassword = new MasterPassword("StrongMasterPassword1!");
        var record = SecretRecord.Create("GitHub", "user@example.com", "Password123!", "primary", timestamp);
        var vault = new SecretVault([record], timestamp, timestamp);

        // Act
        var encryptedDocument = VaultCryptographyService.EncryptVault(vault, masterPassword);
        var decryptedVault = VaultCryptographyService.DecryptVault(encryptedDocument, masterPassword);

        // Assert
        decryptedVault.Count.Should().Be(1);
        decryptedVault.GetSecret(0).Source.Value.Should().Be("GitHub");
        decryptedVault.GetSecret(0).Login.Value.Should().Be("user@example.com");
        decryptedVault.GetSecret(0).Password.Value.Should().Be("Password123!");
        decryptedVault.GetSecret(0).Notes.Value.Should().Be("primary");
    }

    [Fact(DisplayName = "Decrypt should throw when the master password is incorrect")]
    [Trait("Category", "Unit")]
    public void DecryptShouldThrowWhenTheMasterPasswordIsIncorrect()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var correctPassword = new MasterPassword("StrongMasterPassword1!");
        var wrongPassword = new MasterPassword("AnotherStrongPassword1!");
        var vault = SecretVault.CreateEmpty(timestamp);
        var encryptedDocument = VaultCryptographyService.EncryptVault(vault, correctPassword);

        // Act
        var action = () => VaultCryptographyService.DecryptVault(encryptedDocument, wrongPassword);

        // Assert
        var exception = action.Should().Throw<InvalidDataException>().Which;
        exception.Message.Should().Be("The provided master password is incorrect or the vault file is corrupted.");
    }

    [Fact(DisplayName = "Decrypt should throw when the envelope version is unsupported")]
    [Trait("Category", "Unit")]
    public void DecryptShouldThrowWhenTheEnvelopeVersionIsUnsupported()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var masterPassword = new MasterPassword("StrongMasterPassword1!");
        var encryptedDocument = VaultCryptographyService.EncryptVault(SecretVault.CreateEmpty(timestamp), masterPassword);
        encryptedDocument.Version = 2;

        // Act
        var action = () => VaultCryptographyService.DecryptVault(encryptedDocument, masterPassword);

        // Assert
        var exception = action.Should().Throw<InvalidDataException>().Which;
        exception.Message.Should().Be("Vault file version is not supported.");
    }
}
