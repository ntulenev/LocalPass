using Abstractions;

namespace LocalPass.Utility;

/// <summary>
/// Application entry point wrapper for LocalPass.
/// </summary>
/// <param name="workflow">Workflow that drives the application runtime.</param>
public sealed class Application(ILocalPassWorkflow workflow) : IApplication
{
    /// <summary>
    /// Runs the application workflow.
    /// </summary>
    /// <param name="cancellationToken">Token used to stop the application.</param>
    public Task RunAsync(CancellationToken cancellationToken)
        => _workflow.RunAsync(cancellationToken);

    private readonly ILocalPassWorkflow _workflow = workflow
        ?? throw new ArgumentNullException(nameof(workflow));
}
