using Abstractions;

namespace Logic;

/// <summary>
/// Coordinates vault access and UI execution for LocalPass.
/// </summary>
/// <param name="vaultAccessCoordinator">Coordinator used for unlock and first-run flows.</param>
/// <param name="consoleRenderer">Renderer used to display the interactive vault UI.</param>
public sealed class LocalPassWorkflow(
    IVaultAccessCoordinator vaultAccessCoordinator,
    ISecretVaultConsoleRenderer consoleRenderer) : ILocalPassWorkflow
{
    /// <summary>
    /// Runs the LocalPass workflow.
    /// </summary>
    /// <param name="cancellationToken">Token used to stop the workflow.</param>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var session = await _vaultAccessCoordinator.OpenAsync(cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return;
        }

        await _consoleRenderer.RunAsync(session, cancellationToken).ConfigureAwait(false);
    }

    private readonly IVaultAccessCoordinator _vaultAccessCoordinator = vaultAccessCoordinator
        ?? throw new ArgumentNullException(nameof(vaultAccessCoordinator));
    private readonly ISecretVaultConsoleRenderer _consoleRenderer = consoleRenderer
        ?? throw new ArgumentNullException(nameof(consoleRenderer));
}
