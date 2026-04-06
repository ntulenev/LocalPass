using FluentAssertions;

using Infrastructure.Dialogs;

namespace Infrastructure.Tests.Dialogs;

public sealed class MasterPasswordDialogTests
{
    [Fact(DisplayName = "GetTitle should return the rotation dialog title")]
    [Trait("Category", "Unit")]
    public void GetTitleShouldReturnTheRotationDialogTitle()
    {
        MasterPasswordDialog.Title.Should().Be("Rotate Master Key");
    }

    [Fact(DisplayName = "GetRequirementsText should return the password policy text")]
    [Trait("Category", "Unit")]
    public void GetRequirementsTextShouldReturnThePasswordPolicyText()
    {
        MasterPasswordDialog.RequirementsText
            .Should()
            .Be("16+ chars, uppercase, lowercase, digit, symbol, no whitespace");
    }

    [Fact(DisplayName = "CreateValidatedMasterPassword should require matching confirmation")]
    [Trait("Category", "Unit")]
    public void CreateValidatedMasterPasswordShouldRequireMatchingConfirmation()
    {
        var action = () => MasterPasswordDialog.CreateValidatedMasterPassword(
            "StrongMasterPassword1!",
            "DifferentStrongPassword1!");

        action.Should()
            .Throw<System.IO.InvalidDataException>()
            .Which.Message.Should().Be("Passwords do not match.");
    }

    [Fact(DisplayName = "CreateValidatedMasterPassword should preserve the accepted password")]
    [Trait("Category", "Unit")]
    public void CreateValidatedMasterPasswordShouldPreserveTheAcceptedPassword()
    {
        var password = MasterPasswordDialog.CreateValidatedMasterPassword(
            "StrongMasterPassword1!",
            "StrongMasterPassword1!");

        password.Value.Should().Be("StrongMasterPassword1!");
    }
}
