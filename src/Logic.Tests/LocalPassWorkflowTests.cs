using Abstractions;

using FluentAssertions;

using Models;

using Moq;

namespace Logic.Tests;

public sealed class LocalPassWorkflowTests
{
    [Fact(DisplayName = "Ctor should throw when vault access coordinator is null")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenVaultAccessCoordinatorIsNull()
    {
        // Arrange
        var renderer = new Mock<ISecretVaultConsoleRenderer>(MockBehavior.Strict);

        // Act
        var action = () => new LocalPassWorkflow(null!, renderer.Object);

        // Assert
        var exception = action.Should().Throw<ArgumentNullException>().Which;
        exception.ParamName.Should().Be("vaultAccessCoordinator");
    }

    [Fact(DisplayName = "RunAsync should exit when session acquisition is cancelled")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncShouldExitWhenSessionAcquisitionIsCancelled()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var coordinator = new Mock<IVaultAccessCoordinator>(MockBehavior.Strict);
        var renderer = new Mock<ISecretVaultConsoleRenderer>(MockBehavior.Strict);
        _ = coordinator
            .Setup(item => item.OpenAsync(cancellationToken))
            .ReturnsAsync((SecretVaultSession?)null);
        var workflow = new LocalPassWorkflow(coordinator.Object, renderer.Object);

        // Act
        await workflow.RunAsync(cancellationToken);

        // Assert
        coordinator.Verify(item => item.OpenAsync(cancellationToken), Times.Once);
        renderer.VerifyNoOtherCalls();
    }

    [Fact(DisplayName = "RunAsync should delegate to renderer when a session is available")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncShouldDelegateToRendererWhenASessionIsAvailable()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var session = new SecretVaultSession(
            SecretVault.CreateEmpty(DateTimeOffset.UtcNow),
            new MasterPassword("StrongMasterPassword1!"));
        var coordinator = new Mock<IVaultAccessCoordinator>(MockBehavior.Strict);
        var renderer = new Mock<ISecretVaultConsoleRenderer>(MockBehavior.Strict);
        _ = coordinator
            .Setup(item => item.OpenAsync(cancellationToken))
            .ReturnsAsync(session);
        _ = renderer
            .Setup(item => item.RunAsync(session, cancellationToken))
            .Returns(Task.CompletedTask);
        var workflow = new LocalPassWorkflow(coordinator.Object, renderer.Object);

        // Act
        await workflow.RunAsync(cancellationToken);

        // Assert
        coordinator.Verify(item => item.OpenAsync(cancellationToken), Times.Once);
        renderer.Verify(item => item.RunAsync(session, cancellationToken), Times.Once);
    }
}
