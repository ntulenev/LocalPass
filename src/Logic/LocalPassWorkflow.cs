using Abstractions;

namespace Logic;

/// <summary>
/// Coordinates vault access and UI execution for LocalPass.
/// </summary>
/// <param name="vaultAccessCoordinator">Coordinator used for unlock and first-run flows.</param>
/// <param name="consoleSessionFactory">Factory that wraps unlocked sessions for the console UI.</param>
/// <param name="consoleRenderer">Renderer used to display the interactive vault UI.</param>
public sealed class LocalPassWorkflow(
    IVaultAccessCoordinator vaultAccessCoordinator,
    ILocalPassConsoleSessionFactory consoleSessionFactory,
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

        var consoleSession = _consoleSessionFactory.Create(session);
        await _consoleRenderer.RunAsync(consoleSession, cancellationToken).ConfigureAwait(false);
    }

    private readonly IVaultAccessCoordinator _vaultAccessCoordinator = vaultAccessCoordinator
        ?? throw new ArgumentNullException(nameof(vaultAccessCoordinator));
    private readonly ILocalPassConsoleSessionFactory _consoleSessionFactory = consoleSessionFactory
        ?? throw new ArgumentNullException(nameof(consoleSessionFactory));
    private readonly ISecretVaultConsoleRenderer _consoleRenderer = consoleRenderer
        ?? throw new ArgumentNullException(nameof(consoleRenderer));
}
