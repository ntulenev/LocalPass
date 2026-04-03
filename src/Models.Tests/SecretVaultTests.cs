using FluentAssertions;

using Models;

namespace Models.Tests;

public sealed class SecretVaultTests
{
    [Fact(DisplayName = "WithEntry should keep entries sorted by source and login")]
    [Trait("Category", "Unit")]
    public void WithEntryShouldKeepEntriesSortedBySourceAndLogin()
    {
        // Arrange
        var createdUtc = new DateTimeOffset(2026, 4, 3, 10, 0, 0, TimeSpan.Zero);
        var vault = SecretVault.CreateEmpty(createdUtc);
        var first = SecretRecord.Create("Zeta", "second", "Password123!", null, createdUtc);
        var second = SecretRecord.Create("Alpha", "first", "Password123!", null, createdUtc);

        // Act
        var updatedVault = vault
            .WithEntry(first, createdUtc)
            .WithEntry(second, createdUtc);

        // Assert
        updatedVault.Entries.Select(item => item.Source.Value)
            .Should()
            .ContainInOrder("Alpha", "Zeta");
    }

    [Fact(DisplayName = "WithoutEntry should throw when the identifier does not exist")]
    [Trait("Category", "Unit")]
    public void WithoutEntryShouldThrowWhenTheIdentifierDoesNotExist()
    {
        // Arrange
        var createdUtc = new DateTimeOffset(2026, 4, 3, 10, 0, 0, TimeSpan.Zero);
        var vault = SecretVault.CreateEmpty(createdUtc);

        // Act
        var action = () => vault.WithoutEntry(Guid.NewGuid(), createdUtc);

        // Assert
        var exception = action.Should().Throw<InvalidDataException>().Which;
        exception.Message.Should().Be("Secret record was not found.");
    }
}
