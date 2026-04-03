using FluentAssertions;

using Models;

namespace Models.Tests;

public sealed class LocalPassOperationResultTests
{
    [Fact(DisplayName = "Ctor should throw when session is null")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenSessionIsNull()
    {
        // Arrange
        var action = () => new LocalPassOperationResult(null!, "Saved.");

        // Act
        var exception = action.Should().Throw<ArgumentNullException>().Which;

        // Assert
        exception.ParamName.Should().Be("session");
    }

    [Fact(DisplayName = "Ctor should throw when status message is empty")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenStatusMessageIsEmpty()
    {
        // Arrange
        var session = CreateSession();
        var action = () => new LocalPassOperationResult(session, " ");

        // Act
        var exception = action.Should().Throw<ArgumentException>().Which;

        // Assert
        exception.ParamName.Should().Be("statusMessage");
    }

    [Fact(DisplayName = "Ctor should keep the provided session, status message, and preferred selection")]
    [Trait("Category", "Unit")]
    public void CtorShouldKeepTheProvidedSessionStatusMessageAndPreferredSelection()
    {
        // Arrange
        var session = CreateSession();
        var preferredSelectionId = Guid.NewGuid();

        // Act
        var result = new LocalPassOperationResult(session, "Saved GitHub.", preferredSelectionId);

        // Assert
        result.Session.Should().BeSameAs(session);
        result.StatusMessage.Should().Be("Saved GitHub.");
        result.PreferredSelectionId.Should().Be(preferredSelectionId);
    }

    private static SecretVaultSession CreateSession()
        => new(
            SecretVault.CreateEmpty(new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero)),
            new MasterPassword("StrongMasterPassword1!"));
}
