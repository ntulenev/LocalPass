using FluentAssertions;

using Models;

namespace Models.Tests;

public sealed class SecretVaultAdditionalTests
{
    [Fact(DisplayName = "FindIndex should return the matching entry index or minus one")]
    [Trait("Category", "Unit")]
    public void FindIndexShouldReturnTheMatchingEntryIndexOrMinusOne()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 4, 3, 10, 0, 0, TimeSpan.Zero);
        var first = SecretRecord.Create("Alpha", "first", "Password123!", null, timestamp);
        var second = SecretRecord.Create("Beta", "second", "Password123!", null, timestamp);
        var vault = new SecretVault([first, second], timestamp, timestamp);

        // Act
        var existingIndex = vault.FindIndex(second.Id);
        var missingIndex = vault.FindIndex(Guid.NewGuid());

        // Assert
        existingIndex.Should().Be(1);
        missingIndex.Should().Be(-1);
    }

    [Fact(DisplayName = "CreateEmpty should reject non-UTC timestamps")]
    [Trait("Category", "Unit")]
    public void CreateEmptyShouldRejectNonUtcTimestamps()
    {
        // Arrange
        var action = () => SecretVault.CreateEmpty(new DateTimeOffset(2026, 4, 3, 10, 0, 0, TimeSpan.FromHours(2)));

        // Act
        var exception = action.Should().Throw<InvalidDataException>().Which;

        // Assert
        exception.Message.Should().Be("Vault created timestamp must be in UTC.");
    }

    [Fact(DisplayName = "WithEntry should replace an existing record with the same identifier")]
    [Trait("Category", "Unit")]
    public void WithEntryShouldReplaceAnExistingRecordWithTheSameIdentifier()
    {
        // Arrange
        var createdUtc = new DateTimeOffset(2026, 4, 3, 10, 0, 0, TimeSpan.Zero);
        var updatedUtc = createdUtc.AddHours(1);
        var record = SecretRecord.Create("GitHub", "first", "Password123!", null, createdUtc);
        var replacement = new SecretRecord(
            record.Id,
            new SecretSource("GitHub"),
            new SecretLogin("second"),
            new SecretPassword("Password123!Updated"),
            new SecretNotes("rotated"),
            record.CreatedUtc,
            updatedUtc);
        var vault = new SecretVault([record], createdUtc, createdUtc);

        // Act
        var updatedVault = vault.WithEntry(replacement, updatedUtc);

        // Assert
        updatedVault.Count.Should().Be(1);
        updatedVault.GetSecret(0).Id.Should().Be(record.Id);
        updatedVault.GetSecret(0).Login.Value.Should().Be("second");
        updatedVault.GetSecret(0).Password.Value.Should().Be("Password123!Updated");
        updatedVault.GetSecret(0).Notes.Value.Should().Be("rotated");
        updatedVault.UpdatedUtc.Should().Be(updatedUtc);
    }
}
