using Abstractions;

using System.Text;

namespace Infrastructure;

/// <summary>
/// Reads secret values from the console while displaying the current keyboard layout.
/// </summary>
public sealed class ConsoleSecretInputPrompter : ISecretInputPrompter
{
    private const int PromptRefreshDelayMilliseconds = 75;

    /// <summary>
    /// Initializes a new secret input prompter.
    /// </summary>
    /// <param name="keyboardLayoutProvider">Keyboard layout provider.</param>
    public ConsoleSecretInputPrompter(KeyboardLayoutProvider keyboardLayoutProvider)
    {
        _keyboardLayoutProvider = keyboardLayoutProvider
            ?? throw new ArgumentNullException(nameof(keyboardLayoutProvider));
    }

    /// <inheritdoc />
    public string? ReadSecret(string promptLabel)
    {
        var builder = new StringBuilder();
        var currentLayout = _keyboardLayoutProvider.GetDisplayName();
        var renderedPrompt = BuildPrompt(promptLabel, currentLayout);
        RenderPrompt(renderedPrompt, builder.Length, renderedPrompt.Length);

        while (true)
        {
            var latestLayout = _keyboardLayoutProvider.GetDisplayName();
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

    /// <inheritdoc />
    public void ShowRetry(string message)
    {
        Console.WriteLine();
        Console.WriteLine(message);
        Console.WriteLine("Press Enter to try again.");
        _ = Console.ReadLine();
    }

    private static string BuildPrompt(string promptLabel, string keyboardLayout)
        => $"{promptLabel} [{keyboardLayout}]: ";

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

    private readonly KeyboardLayoutProvider _keyboardLayoutProvider;
}
