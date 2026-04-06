using FluentAssertions;

using Infrastructure.Dialogs;

using Models;

namespace Infrastructure.Tests.Dialogs;

public sealed class SecretEditorDialogTests
{
    [Fact(DisplayName = "GetTitle should return Inject Secret when creating a new record")]
    [Trait("Category", "Unit")]
    public void GetTitleShouldReturnInjectSecretWhenCreatingANewRecord()
    {
        SecretEditorDialog.GetTitle(null).Should().Be("Inject Secret");
    }

    [Fact(DisplayName = "GetTitle should return Patch Secret when editing an existing record")]
    [Trait("Category", "Unit")]
    public void GetTitleShouldReturnPatchSecretWhenEditingAnExistingRecord()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var secret = SecretRecord.Create("GitHub", "user@example.com", "Password123!", null, timestamp);

        SecretEditorDialog.GetTitle(secret).Should().Be("Patch Secret");
    }

    [Fact(DisplayName = "CreateValidatedInput should preserve normalized values")]
    [Trait("Category", "Unit")]
    public void CreateValidatedInputShouldPreserveNormalizedValues()
    {
        var input = SecretEditorDialog.CreateValidatedInput(
            " GitHub ",
            " user@example.com ",
            "Password123!",
            " primary ");

        input.Source.Value.Should().Be("GitHub");
        input.Login.Value.Should().Be("user@example.com");
        input.Password.Value.Should().Be("Password123!");
        input.Notes.Value.Should().Be("primary");
    }
}
