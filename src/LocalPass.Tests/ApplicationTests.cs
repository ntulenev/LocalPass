using Abstractions;

using FluentAssertions;

using LocalPass.Utility;

using Moq;

namespace LocalPass.Tests;

public sealed class ApplicationTests
{
    [Fact(DisplayName = "Ctor should throw when workflow is null")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenWorkflowIsNull()
    {
        // Arrange
        var action = () => new LocalPassApplication(null!);

        // Act
        var exception = action.Should().Throw<ArgumentNullException>().Which;

        // Assert
        exception.ParamName.Should().Be("workflow");
    }

    [Fact(DisplayName = "RunAsync should delegate to workflow")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncShouldDelegateToWorkflow()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;
        var workflow = new Mock<ILocalPassWorkflow>(MockBehavior.Strict);
        _ = workflow
            .Setup(item => item.RunAsync(cancellationToken))
            .Returns(Task.CompletedTask);
        var application = new LocalPassApplication(workflow.Object);

        // Act
        await application.RunAsync(cancellationToken);

        // Assert
        workflow.Verify(item => item.RunAsync(cancellationToken), Times.Once);
    }
}
