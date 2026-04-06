using Abstractions;

using FluentAssertions;

using Models;

using Moq;

namespace Infrastructure.Tests;

public sealed class LocalPassConsoleControllerTests
{
    [Fact(DisplayName = "BuildScreenState should clamp selection to the last available item")]
    [Trait("Category", "Unit")]
    public void BuildScreenStateShouldClampSelectionToTheLastAvailableItem()
    {
        var session = BuildSessionWithState(
            [SecretRecord.Create("Alpha", "first", "Password123!", null, Timestamp)],
            "Ready.");
        var controller = new LocalPassConsoleController(
            session.Object,
            _ => null,
            () => null,
            _ => false);

        var state = controller.BuildScreenState(previousSelection: 10);

        state.SelectedIndex.Should().Be(0);
        state.StatusText.Should().Be("[ READY ] Ready.");
        state.DetailsTitle.Should().Be("Payload :: Alpha");
    }

    [Fact(DisplayName = "AddSecret should refresh and preserve the preferred selection from the session")]
    [Trait("Category", "Unit")]
    public void AddSecretShouldRefreshAndPreserveThePreferredSelectionFromTheSession()
    {
        var input = SecretEditorInput.Create("GitHub", "user@example.com", "Password123!", "primary");
        var createdRecord = input.ToRecord(Timestamp);
        var session = BuildSessionWithState([], "Ready.");
        _ = session
            .Setup(item => item.AddSecret(input))
            .Returns(new LocalPassOperationResult(
                new SecretVault([createdRecord], Timestamp, Timestamp),
                "Saved GitHub.",
                createdRecord.Id));
        var controller = new LocalPassConsoleController(
            session.Object,
            _ => input,
            () => null,
            _ => false);

        var result = controller.AddSecret();

        result.ShouldRefresh.Should().BeTrue();
        result.PreferredSelectionId.Should().Be(createdRecord.Id);
        session.Verify(item => item.AddSecret(input), Times.Once);
    }

    [Fact(DisplayName = "DeleteSecret should update the status when deletion is cancelled")]
    [Trait("Category", "Unit")]
    public void DeleteSecretShouldUpdateTheStatusWhenDeletionIsCancelled()
    {
        var record = SecretRecord.Create("GitHub", "user@example.com", "Password123!", null, Timestamp);
        var session = BuildSessionWithState([record], "Ready.");
        var controller = new LocalPassConsoleController(
            session.Object,
            _ => null,
            () => null,
            _ => false);

        var result = controller.DeleteSecret(0);
        var state = controller.BuildScreenState(previousSelection: 0);

        result.ShouldRefresh.Should().BeTrue();
        state.StatusText.Should().Be("[ READY ] Delete cancelled.");
        session.Verify(item => item.DeleteSecret(It.IsAny<Guid>()), Times.Never);
    }

    [Fact(DisplayName = "TogglePasswordVisibility should refresh and update the details text")]
    [Trait("Category", "Unit")]
    public void TogglePasswordVisibilityShouldRefreshAndUpdateTheDetailsText()
    {
        var record = SecretRecord.Create("GitHub", "user@example.com", "Password123!", null, Timestamp);
        var session = BuildSessionWithState([record], "Ready.");
        var controller = new LocalPassConsoleController(
            session.Object,
            _ => null,
            () => null,
            _ => false);

        var result = controller.TogglePasswordVisibility();
        var state = controller.BuildScreenState(previousSelection: 0);

        result.ShouldRefresh.Should().BeTrue();
        state.StatusText.Should().Be("[ READY ] Passwords are visible.");
        state.DetailsText.Should().Contain("PASSWORD  Password123!");
    }

    [Fact(DisplayName = "OpenStorageDirectory should return an error result when the session throws")]
    [Trait("Category", "Unit")]
    public void OpenStorageDirectoryShouldReturnAnErrorResultWhenTheSessionThrows()
    {
        var session = BuildSessionWithState([], "Ready.");
        _ = session
            .Setup(item => item.OpenStorageDirectory())
            .Throws(new InvalidOperationException("Cannot open folder."));
        var controller = new LocalPassConsoleController(
            session.Object,
            _ => null,
            () => null,
            _ => false);

        var result = controller.OpenStorageDirectory();
        var state = controller.BuildScreenState(previousSelection: 0);

        result.ShouldRefresh.Should().BeTrue();
        result.ErrorDialogTitle.Should().Be("Open folder failed");
        result.ErrorDialogMessage.Should().Be("Cannot open folder.");
        state.StatusText.Should().Be("[ READY ] Open folder failed: Cannot open folder.");
    }

    private static Mock<ILocalPassConsoleSession> BuildSessionWithState(
        IReadOnlyList<SecretRecord> records,
        string statusMessage)
    {
        var vault = new SecretVault(records, Timestamp, Timestamp);
        var session = new Mock<ILocalPassConsoleSession>(MockBehavior.Strict);
        _ = session.SetupGet(item => item.CurrentVault).Returns(vault);
        _ = session.SetupGet(item => item.CurrentStatusMessage).Returns(statusMessage);
        return session;
    }

    private static readonly DateTimeOffset Timestamp = new(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
}
