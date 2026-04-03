using FluentAssertions;

using Models;

namespace Models.Tests;

public sealed class SecretValueObjectTests
{
    [Fact(DisplayName = "SecretSource should trim the input and compare by normalized value")]
    [Trait("Category", "Unit")]
    public void SecretSourceShouldTrimTheInputAndCompareByNormalizedValue()
    {
        // Arrange
        var left = new SecretSource(" GitHub ");
        var right = new SecretSource("GitHub");

        // Act
        var areEqual = left.Equals(right);

        // Assert
        left.Value.Should().Be("GitHub");
        left.ToString().Should().Be("GitHub");
        areEqual.Should().BeTrue();
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact(DisplayName = "SecretLogin should reject blank values")]
    [Trait("Category", "Unit")]
    public void SecretLoginShouldRejectBlankValues()
    {
        // Arrange
        var action = () => new SecretLogin("   ");

        // Act
        var exception = action.Should().Throw<InvalidDataException>().Which;

        // Assert
        exception.Message.Should().Be("Secret login is required.");
    }

    [Fact(DisplayName = "SecretPassword should redact its string representation")]
    [Trait("Category", "Unit")]
    public void SecretPasswordShouldRedactItsStringRepresentation()
    {
        // Arrange
        var password = new SecretPassword("Password123!");

        // Act
        var rendered = password.ToString();

        // Assert
        rendered.Should().Be("[REDACTED]");
        password.Value.Should().Be("Password123!");
    }

    [Fact(DisplayName = "SecretPassword should reject whitespace-only values")]
    [Trait("Category", "Unit")]
    public void SecretPasswordShouldRejectWhitespaceOnlyValues()
    {
        // Arrange
        var action = () => new SecretPassword("   ");

        // Act
        var exception = action.Should().Throw<InvalidDataException>().Which;

        // Assert
        exception.Message.Should().Be("Secret password is required.");
    }

    [Fact(DisplayName = "SecretNotes should trim values and report when content is absent")]
    [Trait("Category", "Unit")]
    public void SecretNotesShouldTrimValuesAndReportWhenContentIsAbsent()
    {
        // Arrange
        var notes = new SecretNotes("  primary account  ");
        var emptyNotes = new SecretNotes("   ");

        // Act
        var areEqual = notes.Equals(new SecretNotes("primary account"));

        // Assert
        notes.Value.Should().Be("primary account");
        notes.HasValue.Should().BeTrue();
        notes.ToString().Should().Be("primary account");
        areEqual.Should().BeTrue();
        emptyNotes.Value.Should().Be(string.Empty);
        emptyNotes.HasValue.Should().BeFalse();
    }
}
