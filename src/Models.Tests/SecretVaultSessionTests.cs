using FluentAssertions;

using Models;

namespace Models.Tests;

public sealed class SecretVaultSessionTests
{
    [Fact(DisplayName = "WithVault should return a new session with the provided vault and keep the master password")]
    [Trait("Category", "Unit")]
    public void WithVaultShouldReturnANewSessionWithTheProvidedVaultAndKeepTheMasterPassword()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var originalVault = SecretVault.CreateEmpty(timestamp);
        var updatedVault = new SecretVault(
            [SecretRecord.Create("GitHub", "user@example.com", "Password123!", null, timestamp)],
            timestamp,
            timestamp);
        var session = new SecretVaultSession(originalVault, new MasterPassword("StrongMasterPassword1!"));

        // Act
        var updatedSession = session.WithVault(updatedVault);

        // Assert
        updatedSession.Should().NotBeSameAs(session);
        updatedSession.Vault.Should().BeSameAs(updatedVault);
        updatedSession.MasterPassword.Should().BeSameAs(session.MasterPassword);
    }

    [Fact(DisplayName = "WithMasterPassword should return a new session with the provided master password and keep the vault")]
    [Trait("Category", "Unit")]
    public void WithMasterPasswordShouldReturnANewSessionWithTheProvidedMasterPasswordAndKeepTheVault()
    {
        // Arrange
        var vault = SecretVault.CreateEmpty(new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero));
        var session = new SecretVaultSession(vault, new MasterPassword("StrongMasterPassword1!"));
        var nextPassword = new MasterPassword("AnotherStrongPassword1!");

        // Act
        var updatedSession = session.WithMasterPassword(nextPassword);

        // Assert
        updatedSession.Should().NotBeSameAs(session);
        updatedSession.Vault.Should().BeSameAs(vault);
        updatedSession.MasterPassword.Should().BeSameAs(nextPassword);
    }
}
