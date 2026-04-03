using Abstractions;

using Models;

using System.IO;

namespace Infrastructure;

/// <summary>
/// Handles first-run setup and unlock prompts by using the system console.
/// </summary>
public sealed class TerminalVaultAccessCoordinator : IVaultAccessCoordinator
{
    private const int MaxUnlockAttempts = 3;

    /// <summary>
    /// Initializes a new vault access coordinator.
    /// </summary>
    /// <param name="vaultStore">Encrypted vault store.</param>
    /// <param name="secretInputPrompter">Secret input prompter used for password entry and retry messaging.</param>
    /// <param name="vaultAccessScreen">Console screen used to render access-flow instructions.</param>
    public TerminalVaultAccessCoordinator(
        ISecretVaultStore vaultStore,
        ISecretInputPrompter secretInputPrompter,
        IVaultAccessScreen vaultAccessScreen)
    {
        _vaultStore = vaultStore ?? throw new ArgumentNullException(nameof(vaultStore));
        _secretInputPrompter = secretInputPrompter
            ?? throw new ArgumentNullException(nameof(secretInputPrompter));
        _vaultAccessScreen = vaultAccessScreen
            ?? throw new ArgumentNullException(nameof(vaultAccessScreen));
    }

    /// <inheritdoc />
    public Task<SecretVaultSession?> OpenAsync(CancellationToken cancellationToken)
        => Task.FromResult(
            _vaultStore.Exists()
                ? UnlockExistingVault(cancellationToken)
                : CreateNewVault(cancellationToken));

    private SecretVaultSession? CreateNewVault(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _vaultAccessScreen.ShowCreateVaultPrompt();

            var password = _secretInputPrompter.ReadSecret("Master password");
            if (password is null)
            {
                return null;
            }

            var confirmation = _secretInputPrompter.ReadSecret("Confirm password");
            if (confirmation is null)
            {
                return null;
            }

            if (!string.Equals(password, confirmation, StringComparison.Ordinal))
            {
                _secretInputPrompter.ShowRetry("Passwords do not match.");
                continue;
            }

            try
            {
                return _vaultStore.CreateNew(new MasterPassword(password));
            }
            catch (InvalidDataException exception)
            {
                _secretInputPrompter.ShowRetry(exception.Message);
            }
        }

        return null;
    }

    private SecretVaultSession? UnlockExistingVault(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxUnlockAttempts; attempt++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            _vaultAccessScreen.ShowUnlockPrompt(attempt, MaxUnlockAttempts);

            var password = _secretInputPrompter.ReadSecret("Master password");
            if (password is null)
            {
                return null;
            }

            try
            {
                return _vaultStore.Open(new MasterPassword(password));
            }
            catch (InvalidDataException exception)
            {
                _secretInputPrompter.ShowRetry(exception.Message);
            }
        }

        _vaultAccessScreen.ShowUnlockAborted();
        return null;
    }

    private readonly IVaultAccessScreen _vaultAccessScreen;
    private readonly ISecretInputPrompter _secretInputPrompter;
    private readonly ISecretVaultStore _vaultStore;
}
