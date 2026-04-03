using Abstractions;

using FluentAssertions;

using Models;

using Moq;

namespace Infrastructure.Tests;

[Collection("Console")]
public sealed class TerminalVaultAccessCoordinatorTests
{
    [Fact(DisplayName = "OpenAsync should create a new vault when no vault file exists")]
    [Trait("Category", "Unit")]
    public async Task OpenAsyncShouldCreateANewVaultWhenNoVaultFileExists()
    {
        // Arrange
        var session = CreateSession();
        var store = new Mock<ISecretVaultStore>(MockBehavior.Strict);
        var prompter = new Mock<ISecretInputPrompter>(MockBehavior.Strict);
        var screen = new Mock<IVaultAccessScreen>(MockBehavior.Strict);
        _ = store.Setup(item => item.Exists()).Returns(false);
        screen.Setup(item => item.ShowCreateVaultPrompt());
        _ = prompter.Setup(item => item.ReadSecret("Master password")).Returns("StrongMasterPassword1!");
        _ = prompter.Setup(item => item.ReadSecret("Confirm password")).Returns("StrongMasterPassword1!");
        _ = store
            .Setup(item => item.CreateNew(It.Is<MasterPassword>(value => value.Value == "StrongMasterPassword1!")))
            .Returns(session);
        var coordinator = new TerminalVaultAccessCoordinator(store.Object, prompter.Object, screen.Object);

        // Act
        var result = await coordinator.OpenAsync(CancellationToken.None);

        // Assert
        result.Should().BeSameAs(session);
        store.Verify(item => item.CreateNew(It.IsAny<MasterPassword>()), Times.Once);
        prompter.Verify(item => item.ShowRetry(It.IsAny<string>()), Times.Never);
        screen.Verify(item => item.ShowCreateVaultPrompt(), Times.Once);
    }

    [Fact(DisplayName = "OpenAsync should retry vault creation when password confirmation does not match")]
    [Trait("Category", "Unit")]
    public async Task OpenAsyncShouldRetryVaultCreationWhenPasswordConfirmationDoesNotMatch()
    {
        // Arrange
        var session = CreateSession();
        var store = new Mock<ISecretVaultStore>(MockBehavior.Strict);
        var prompter = new Mock<ISecretInputPrompter>(MockBehavior.Strict);
        var screen = new Mock<IVaultAccessScreen>(MockBehavior.Strict);
        _ = store.Setup(item => item.Exists()).Returns(false);
        screen.Setup(item => item.ShowCreateVaultPrompt());
        _ = prompter.SetupSequence(item => item.ReadSecret("Master password"))
            .Returns("StrongMasterPassword1!")
            .Returns("StrongMasterPassword1!");
        _ = prompter.SetupSequence(item => item.ReadSecret("Confirm password"))
            .Returns("MismatchPassword1!")
            .Returns("StrongMasterPassword1!");
        prompter.Setup(item => item.ShowRetry("Passwords do not match."));
        _ = store
            .Setup(item => item.CreateNew(It.Is<MasterPassword>(value => value.Value == "StrongMasterPassword1!")))
            .Returns(session);
        var coordinator = new TerminalVaultAccessCoordinator(store.Object, prompter.Object, screen.Object);

        // Act
        var result = await coordinator.OpenAsync(CancellationToken.None);

        // Assert
        result.Should().BeSameAs(session);
        prompter.Verify(item => item.ShowRetry("Passwords do not match."), Times.Once);
        store.Verify(item => item.CreateNew(It.IsAny<MasterPassword>()), Times.Once);
        screen.Verify(item => item.ShowCreateVaultPrompt(), Times.Exactly(2));
    }

    [Fact(DisplayName = "OpenAsync should retry unlock after a failed password attempt")]
    [Trait("Category", "Unit")]
    public async Task OpenAsyncShouldRetryUnlockAfterAFailedPasswordAttempt()
    {
        // Arrange
        var session = CreateSession();
        var store = new Mock<ISecretVaultStore>(MockBehavior.Strict);
        var prompter = new Mock<ISecretInputPrompter>(MockBehavior.Strict);
        var screen = new Mock<IVaultAccessScreen>(MockBehavior.Strict);
        _ = store.Setup(item => item.Exists()).Returns(true);
        screen.Setup(item => item.ShowUnlockPrompt(It.IsAny<int>(), 3));
        _ = prompter.SetupSequence(item => item.ReadSecret("Master password"))
            .Returns("WrongStrongPassword1!")
            .Returns("StrongMasterPassword1!");
        _ = store
            .Setup(item => item.Open(It.Is<MasterPassword>(value => value.Value == "WrongStrongPassword1!")))
            .Throws(new InvalidDataException("Unlock failed."));
        _ = store
            .Setup(item => item.Open(It.Is<MasterPassword>(value => value.Value == "StrongMasterPassword1!")))
            .Returns(session);
        prompter.Setup(item => item.ShowRetry("Unlock failed."));
        var coordinator = new TerminalVaultAccessCoordinator(store.Object, prompter.Object, screen.Object);

        // Act
        var result = await coordinator.OpenAsync(CancellationToken.None);

        // Assert
        result.Should().BeSameAs(session);
        prompter.Verify(item => item.ShowRetry("Unlock failed."), Times.Once);
        store.Verify(item => item.Open(It.IsAny<MasterPassword>()), Times.Exactly(2));
        screen.Verify(item => item.ShowUnlockPrompt(It.IsAny<int>(), 3), Times.Exactly(2));
    }

    [Fact(DisplayName = "OpenAsync should stop after three failed unlock attempts")]
    [Trait("Category", "Unit")]
    public async Task OpenAsyncShouldStopAfterThreeFailedUnlockAttempts()
    {
        // Arrange
        var store = new Mock<ISecretVaultStore>(MockBehavior.Strict);
        var prompter = new Mock<ISecretInputPrompter>(MockBehavior.Strict);
        var screen = new Mock<IVaultAccessScreen>(MockBehavior.Strict);
        _ = store.Setup(item => item.Exists()).Returns(true);
        screen.Setup(item => item.ShowUnlockPrompt(It.IsAny<int>(), 3));
        screen.Setup(item => item.ShowUnlockAborted());
        _ = prompter.SetupSequence(item => item.ReadSecret("Master password"))
            .Returns("WrongStrongPassword1!")
            .Returns("WrongStrongPassword2!")
            .Returns("WrongStrongPassword3!");
        _ = store
            .Setup(item => item.Open(It.IsAny<MasterPassword>()))
            .Throws(new InvalidDataException("Unlock failed."));
        prompter.Setup(item => item.ShowRetry("Unlock failed."));
        var coordinator = new TerminalVaultAccessCoordinator(store.Object, prompter.Object, screen.Object);

        // Act
        var result = await coordinator.OpenAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
        prompter.Verify(item => item.ShowRetry("Unlock failed."), Times.Exactly(3));
        store.Verify(item => item.Open(It.IsAny<MasterPassword>()), Times.Exactly(3));
        screen.Verify(item => item.ShowUnlockPrompt(It.IsAny<int>(), 3), Times.Exactly(3));
        screen.Verify(item => item.ShowUnlockAborted(), Times.Once);
    }

    [Fact(DisplayName = "OpenAsync should return null when the user cancels password entry")]
    [Trait("Category", "Unit")]
    public async Task OpenAsyncShouldReturnNullWhenTheUserCancelsPasswordEntry()
    {
        // Arrange
        var store = new Mock<ISecretVaultStore>(MockBehavior.Strict);
        var prompter = new Mock<ISecretInputPrompter>(MockBehavior.Strict);
        var screen = new Mock<IVaultAccessScreen>(MockBehavior.Strict);
        _ = store.Setup(item => item.Exists()).Returns(true);
        screen.Setup(item => item.ShowUnlockPrompt(1, 3));
        _ = prompter.Setup(item => item.ReadSecret("Master password")).Returns((string?)null);
        var coordinator = new TerminalVaultAccessCoordinator(store.Object, prompter.Object, screen.Object);

        // Act
        var result = await coordinator.OpenAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
        store.Verify(item => item.Open(It.IsAny<MasterPassword>()), Times.Never);
        prompter.Verify(item => item.ShowRetry(It.IsAny<string>()), Times.Never);
        screen.Verify(item => item.ShowUnlockPrompt(1, 3), Times.Once);
    }

    private static SecretVaultSession CreateSession()
        => new(
            SecretVault.CreateEmpty(new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero)),
            new MasterPassword("StrongMasterPassword1!"));
}
