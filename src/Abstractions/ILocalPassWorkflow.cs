namespace Abstractions;

/// <summary>
/// Coordinates vault access and interactive UI execution.
/// </summary>
public interface ILocalPassWorkflow
{
    /// <summary>
    /// Runs the main LocalPass workflow.
    /// </summary>
    /// <param name="cancellationToken">Token used to stop the workflow.</param>
    /// <returns>A task that completes when the workflow exits.</returns>
    Task RunAsync(CancellationToken cancellationToken);
}
