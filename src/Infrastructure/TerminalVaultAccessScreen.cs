using Abstractions;

namespace Infrastructure;

/// <summary>
/// Writes vault access prompts to the system console.
/// </summary>
public sealed class TerminalVaultAccessScreen : IVaultAccessScreen
{
    /// <inheritdoc />
    public void ShowCreateVaultPrompt()
    {
        Console.Clear();
        Console.WriteLine("LocalPass");
        Console.WriteLine();
        Console.WriteLine("No vault file was found. Create a new master password.");
        Console.WriteLine("Requirements: 16+ chars, upper/lowercase, digit, symbol, no whitespace.");
        Console.WriteLine("Press Esc at any prompt to cancel.");
        Console.WriteLine();
    }

    /// <inheritdoc />
    public void ShowUnlockPrompt(int attempt, int maxAttempts)
    {
        Console.Clear();
        Console.WriteLine("LocalPass");
        Console.WriteLine();
        Console.WriteLine("Unlock the existing vault.");
        Console.WriteLine($"Attempt {attempt} of {maxAttempts}. Press Esc to cancel.");
        Console.WriteLine();
    }

    /// <inheritdoc />
    public void ShowUnlockAborted()
    {
        Console.WriteLine();
        Console.WriteLine("Vault unlock aborted.");
    }
}
