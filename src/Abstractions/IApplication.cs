namespace Abstractions;

/// <summary>
/// Represents the application entry point abstraction.
/// </summary>
public interface IApplication
{
    /// <summary>
    /// Runs the application.
    /// </summary>
    /// <param name="cancellationToken">Token used to stop the application.</param>
    /// <returns>A task that completes when the application exits.</returns>
    Task RunAsync(CancellationToken cancellationToken);
}
