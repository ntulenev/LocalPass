using FluentAssertions;

using Models;

namespace Models.Tests;

public sealed class MasterPasswordAdditionalTests
{
    [Theory(DisplayName = "Ctor should reject passwords that miss a required character class")]
    [InlineData("alllowercasepassword1!", "Master password must contain an uppercase letter.")]
    [InlineData("ALLUPPERCASEPASSWORD1!", "Master password must contain a lowercase letter.")]
    [InlineData("StrongMasterPassword!", "Master password must contain a digit.")]
    [InlineData("StrongMasterPassword12", "Master password must contain a symbol.")]
    [Trait("Category", "Unit")]
    public void CtorShouldRejectPasswordsThatMissARequiredCharacterClass(
        string value,
        string expectedMessage)
    {
        // Arrange
        var action = () => new MasterPassword(value);

        // Act
        var exception = action.Should().Throw<InvalidDataException>().Which;

        // Assert
        exception.Message.Should().Be(expectedMessage);
    }

    [Fact(DisplayName = "ToString should not expose the raw master password")]
    [Trait("Category", "Unit")]
    public void ToStringShouldNotExposeTheRawMasterPassword()
    {
        // Arrange
        var password = new MasterPassword("StrongMasterPassword1!");

        // Act
        var rendered = password.ToString();

        // Assert
        rendered.Should().Be("[REDACTED]");
    }
}
