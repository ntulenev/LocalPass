namespace Abstractions;

/// <summary>
/// Renders the console prompts used by the vault access flow.
/// </summary>
public interface IVaultAccessScreen
{
    /// <summary>
    /// Renders the first-run master password setup instructions.
    /// </summary>
    void ShowCreateVaultPrompt();

    /// <summary>
    /// Renders the unlock instructions for the current attempt.
    /// </summary>
    /// <param name="attempt">Current unlock attempt number.</param>
    /// <param name="maxAttempts">Maximum number of unlock attempts.</param>
    void ShowUnlockPrompt(int attempt, int maxAttempts);

    /// <summary>
    /// Renders the final unlock aborted message.
    /// </summary>
    void ShowUnlockAborted();
}
