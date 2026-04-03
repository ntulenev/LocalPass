using FluentAssertions;

using Models;

namespace Models.Tests;

public sealed class SecretRecordTests
{
    [Fact(DisplayName = "Create should populate timestamps from the supplied UTC value")]
    [Trait("Category", "Unit")]
    public void CreateShouldPopulateTimestampsFromTheSuppliedUtcValue()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 4, 3, 18, 30, 0, TimeSpan.Zero);

        // Act
        var record = SecretRecord.Create(
            "GitHub",
            "user@example.com",
            "Password123!",
            "primary account",
            timestamp);

        // Assert
        record.Source.Value.Should().Be("GitHub");
        record.Login.Value.Should().Be("user@example.com");
        record.Notes.Value.Should().Be("primary account");
        record.CreatedUtc.Should().Be(timestamp);
        record.UpdatedUtc.Should().Be(timestamp);
    }

    [Fact(DisplayName = "Update should preserve identifier and created timestamp")]
    [Trait("Category", "Unit")]
    public void UpdateShouldPreserveIdentifierAndCreatedTimestamp()
    {
        // Arrange
        var createdUtc = new DateTimeOffset(2026, 4, 2, 12, 0, 0, TimeSpan.Zero);
        var updatedUtc = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var record = SecretRecord.Create(
            "GitHub",
            "user@example.com",
            "Password123!",
            null,
            createdUtc);

        // Act
        var updated = record.Update(
            "GitHub",
            "admin@example.com",
            "Password123!Updated",
            "rotated",
            updatedUtc);

        // Assert
        updated.Id.Should().Be(record.Id);
        updated.CreatedUtc.Should().Be(createdUtc);
        updated.UpdatedUtc.Should().Be(updatedUtc);
        updated.Login.Value.Should().Be("admin@example.com");
        updated.Notes.Value.Should().Be("rotated");
    }
}
