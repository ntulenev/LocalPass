using Abstractions;

using Models;

using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Infrastructure;

/// <summary>
/// Handles first-run setup and unlock prompts by using the system console.
/// </summary>
public sealed class TerminalVaultAccessCoordinator : IVaultAccessCoordinator
{
    private const int MaxUnlockAttempts = 3;
    private const int PromptRefreshDelayMilliseconds = 75;

    /// <summary>
    /// Initializes a new vault access coordinator.
    /// </summary>
    /// <param name="vaultStore">Encrypted vault store.</param>
    public TerminalVaultAccessCoordinator(ISecretVaultStore vaultStore)
    {
        _vaultStore = vaultStore ?? throw new ArgumentNullException(nameof(vaultStore));
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
            Console.Clear();
            Console.WriteLine("LocalPass");
            Console.WriteLine();
            Console.WriteLine("No vault file was found. Create a new master password.");
            Console.WriteLine("Requirements: 16+ chars, upper/lowercase, digit, symbol, no whitespace.");
            Console.WriteLine("Press Esc at any prompt to cancel.");
            Console.WriteLine();

            var password = ReadSecret("Master password");
            if (password is null)
            {
                return null;
            }

            var confirmation = ReadSecret("Confirm password");
            if (confirmation is null)
            {
                return null;
            }

            if (!string.Equals(password, confirmation, StringComparison.Ordinal))
            {
                ShowRetry("Passwords do not match.");
                continue;
            }

            try
            {
                return _vaultStore.CreateNew(new MasterPassword(password));
            }
            catch (InvalidDataException exception)
            {
                ShowRetry(exception.Message);
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

            Console.Clear();
            Console.WriteLine("LocalPass");
            Console.WriteLine();
            Console.WriteLine("Unlock the existing vault.");
            Console.WriteLine($"Attempt {attempt} of {MaxUnlockAttempts}. Press Esc to cancel.");
            Console.WriteLine();

            var password = ReadSecret("Master password");
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
                ShowRetry(exception.Message);
            }
        }

        Console.WriteLine();
        Console.WriteLine("Vault unlock aborted.");
        return null;
    }

    private static string? ReadSecret(string promptLabel)
    {
        var builder = new StringBuilder();
        var currentLayout = GetKeyboardLayoutDisplayName();
        var renderedPrompt = BuildPrompt(promptLabel, currentLayout);
        RenderPrompt(renderedPrompt, builder.Length, renderedPrompt.Length);

        while (true)
        {
            var latestLayout = GetKeyboardLayoutDisplayName();
            if (!string.Equals(latestLayout, currentLayout, StringComparison.Ordinal))
            {
                currentLayout = latestLayout;
                renderedPrompt = BuildPrompt(promptLabel, currentLayout);
                RenderPrompt(renderedPrompt, builder.Length, renderedPrompt.Length + builder.Length);
            }

            if (!Console.KeyAvailable)
            {
                Thread.Sleep(PromptRefreshDelayMilliseconds);
                continue;
            }

            var key = Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    return builder.ToString();

                case ConsoleKey.Escape:
                    Console.WriteLine();
                    return null;

                case ConsoleKey.Backspace:
                    if (builder.Length > 0)
                    {
                        builder.Length--;
                        RenderPrompt(renderedPrompt, builder.Length, renderedPrompt.Length + builder.Length + 1);
                    }

                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        builder.Append(key.KeyChar);
                        Console.Write('*');
                    }

                    break;
            }
        }
    }

    private static string BuildPrompt(string promptLabel, string keyboardLayout)
        => $"{promptLabel} [{keyboardLayout}]: ";

    private static string GetKeyboardLayoutDisplayName()
    {
        if (!OperatingSystem.IsWindows())
        {
            return CultureInfo.CurrentCulture.Name;
        }

        try
        {
            var foregroundWindow = GetForegroundWindow();
            var threadIdentifier = foregroundWindow == nint.Zero
                ? 0U
                : GetWindowThreadProcessId(foregroundWindow, nint.Zero);
            var keyboardLayout = GetKeyboardLayout(threadIdentifier);
            var languageIdentifier = unchecked((ushort)keyboardLayout.ToInt64());
            return CultureInfo.GetCultureInfo(languageIdentifier).Name;
        }
        catch (CultureNotFoundException)
        {
            return "unknown";
        }
    }

    private static void RenderPrompt(string prompt, int hiddenCharacterCount, int previousWidth)
    {
        Console.Write('\r');
        if (previousWidth > 0)
        {
            Console.Write(new string(' ', previousWidth));
            Console.Write('\r');
        }

        Console.Write(prompt);
        if (hiddenCharacterCount > 0)
        {
            Console.Write(new string('*', hiddenCharacterCount));
        }
    }

    private static void ShowRetry(string message)
    {
        Console.WriteLine();
        Console.WriteLine(message);
        Console.WriteLine("Press Enter to try again.");
        _ = Console.ReadLine();
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("user32.dll")]
    private static extern nint GetKeyboardLayout(uint idThread);

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint windowHandle, nint processId);

    private readonly ISecretVaultStore _vaultStore;
}
