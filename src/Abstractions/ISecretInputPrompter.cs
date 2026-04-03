namespace Abstractions;

/// <summary>
/// Prompts the user for secret input on the console.
/// </summary>
public interface ISecretInputPrompter
{
    /// <summary>
    /// Reads a secret value from the user.
    /// </summary>
    /// <param name="promptLabel">Prompt label shown to the user.</param>
    /// <returns>The secret value, or <see langword="null"/> when cancelled.</returns>
    string? ReadSecret(string promptLabel);

    /// <summary>
    /// Shows a retry message and waits for user acknowledgement.
    /// </summary>
    /// <param name="message">Message to display.</param>
    void ShowRetry(string message);
}
