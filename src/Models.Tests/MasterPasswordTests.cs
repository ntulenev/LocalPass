using FluentAssertions;

using Models;

namespace Models.Tests;

public sealed class MasterPasswordTests
{
    [Fact(DisplayName = "Ctor should throw when value is shorter than minimum length")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenValueIsShorterThanMinimumLength()
    {
        // Arrange
        var action = () => new MasterPassword("Short1!");

        // Act
        var exception = action.Should().Throw<InvalidDataException>().Which;

        // Assert
        exception.Message.Should().Be("Master password must be at least 16 characters long.");
    }

    [Fact(DisplayName = "Ctor should throw when value contains whitespace")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenValueContainsWhitespace()
    {
        // Arrange
        var action = () => new MasterPassword("Strong Password1!");

        // Act
        var exception = action.Should().Throw<InvalidDataException>().Which;

        // Assert
        exception.Message.Should().Be("Master password must not contain whitespace.");
    }

    [Fact(DisplayName = "Ctor should keep a valid password value")]
    [Trait("Category", "Unit")]
    public void CtorShouldKeepAValidPasswordValue()
    {
        // Arrange
        var password = new MasterPassword("StrongMasterPassword1!");

        // Act
        var value = password.Value;

        // Assert
        value.Should().Be("StrongMasterPassword1!");
    }
}
