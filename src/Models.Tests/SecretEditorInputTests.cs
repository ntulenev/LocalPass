using FluentAssertions;

using Models;

namespace Models.Tests;

public sealed class SecretEditorInputTests
{
    [Fact(DisplayName = "Create should materialize validated domain value objects")]
    [Trait("Category", "Unit")]
    public void CreateShouldMaterializeValidatedDomainValueObjects()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);

        // Act
        var input = SecretEditorInput.Create(" GitHub ", " user@example.com ", "Password123!", " primary ");
        var record = input.ToRecord(timestamp);

        // Assert
        input.Source.Value.Should().Be("GitHub");
        input.Login.Value.Should().Be("user@example.com");
        input.Password.Value.Should().Be("Password123!");
        input.Notes.Value.Should().Be("primary");
        record.Source.Value.Should().Be("GitHub");
        record.Login.Value.Should().Be("user@example.com");
        record.Password.Value.Should().Be("Password123!");
        record.Notes.Value.Should().Be("primary");
    }

    [Fact(DisplayName = "Create should reject empty required fields")]
    [Trait("Category", "Unit")]
    public void CreateShouldRejectEmptyRequiredFields()
    {
        // Arrange
        var action = () => SecretEditorInput.Create(" ", "user@example.com", "Password123!", null);

        // Act
        var exception = action.Should().Throw<InvalidDataException>().Which;

        // Assert
        exception.Message.Should().Be("Secret source is required.");
    }

    [Fact(DisplayName = "ApplyTo should update an existing record by using validated values")]
    [Trait("Category", "Unit")]
    public void ApplyToShouldUpdateAnExistingRecordByUsingValidatedValues()
    {
        // Arrange
        var createdUtc = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var updatedUtc = createdUtc.AddMinutes(5);
        var existingRecord = SecretRecord.Create("GitHub", "user@example.com", "Password123!", null, createdUtc);
        var input = SecretEditorInput.Create("GitHub", "admin@example.com", "NewPassword123!", "rotated");

        // Act
        var updatedRecord = input.ApplyTo(existingRecord, updatedUtc);

        // Assert
        updatedRecord.Id.Should().Be(existingRecord.Id);
        updatedRecord.Login.Value.Should().Be("admin@example.com");
        updatedRecord.Password.Value.Should().Be("NewPassword123!");
        updatedRecord.Notes.Value.Should().Be("rotated");
        updatedRecord.UpdatedUtc.Should().Be(updatedUtc);
    }
}
