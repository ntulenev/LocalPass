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
        var sessionFactory = new Mock<ILocalPassConsoleSessionFactory>(MockBehavior.Strict);
        var renderer = new Mock<ISecretVaultConsoleRenderer>(MockBehavior.Strict);

        // Act
        var action = () => new LocalPassWorkflow(null!, sessionFactory.Object, renderer.Object);

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
        var sessionFactory = new Mock<ILocalPassConsoleSessionFactory>(MockBehavior.Strict);
        var renderer = new Mock<ISecretVaultConsoleRenderer>(MockBehavior.Strict);
        _ = coordinator
            .Setup(item => item.OpenAsync(cancellationToken))
            .ReturnsAsync((SecretVaultSession?)null);
        var workflow = new LocalPassWorkflow(coordinator.Object, sessionFactory.Object, renderer.Object);

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
        var sessionFactory = new Mock<ILocalPassConsoleSessionFactory>(MockBehavior.Strict);
        var appSession = new Mock<ILocalPassConsoleSession>(MockBehavior.Strict);
        var renderer = new Mock<ISecretVaultConsoleRenderer>(MockBehavior.Strict);
        _ = coordinator
            .Setup(item => item.OpenAsync(cancellationToken))
            .ReturnsAsync(session);
        _ = sessionFactory
            .Setup(item => item.Create(session))
            .Returns(appSession.Object);
        _ = renderer
            .Setup(item => item.RunAsync(appSession.Object, cancellationToken))
            .Returns(Task.CompletedTask);
        var workflow = new LocalPassWorkflow(coordinator.Object, sessionFactory.Object, renderer.Object);

        // Act
        await workflow.RunAsync(cancellationToken);

        // Assert
        coordinator.Verify(item => item.OpenAsync(cancellationToken), Times.Once);
        sessionFactory.Verify(item => item.Create(session), Times.Once);
        renderer.Verify(item => item.RunAsync(appSession.Object, cancellationToken), Times.Once);
    }
}
