using Abstractions;

namespace Infrastructure;

/// <summary>
/// Writes vault access prompts to the system console.
/// </summary>
public sealed class TerminalVaultAccessScreen : IVaultAccessScreen
{
    private const int MinimumContentWidth = 44;
    private const int MaximumContentWidth = 84;

    /// <inheritdoc />
    public void ShowCreateVaultPrompt()
        => ShowScreen(
            mode: "Create vault",
            message: "No vault file was found. Create a new master password.",
            detail: "Requirements: 16+ chars, uppercase, lowercase, digit, symbol, no whitespace.",
            footer: "Press Esc at any prompt to cancel.");

    /// <inheritdoc />
    public void ShowUnlockPrompt(int attempt, int maxAttempts)
        => ShowScreen(
            mode: $"Unlock vault [{attempt}/{maxAttempts}]",
            message: "Unlock the existing vault.",
            detail: "Enter the master password for the encrypted vault.",
            footer: "Press Esc to cancel.");

    /// <inheritdoc />
    public void ShowUnlockAborted()
    {
        ApplyTheme();
        Console.WriteLine();
        WriteAccentLine("Vault unlock aborted after the maximum number of attempts.");
    }

    private static void ShowScreen(string mode, string message, string detail, string footer)
    {
        ApplyTheme();
        Console.Clear();

        var contentWidth = GetContentWidth();
        WriteBorder(contentWidth);
        WriteLine(contentWidth, "LocalPass :: Vault Access");
        WriteBorder(contentWidth);
        WriteKeyValueLine(contentWidth, "MODE", mode);
        WriteEmptyLine(contentWidth);
        WriteWrappedText(contentWidth, message);
        WriteWrappedText(contentWidth, detail);
        WriteEmptyLine(contentWidth);
        WriteWrappedText(contentWidth, footer);
        WriteBorder(contentWidth);
        Console.WriteLine();
    }

    private static void ApplyTheme()
    {
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.Green;
    }

    private static int GetContentWidth()
    {
        try
        {
            var candidateWidth = Console.WindowWidth - 4;
            return Math.Clamp(candidateWidth, MinimumContentWidth, MaximumContentWidth);
        }
        catch (IOException)
        {
            return MaximumContentWidth;
        }
    }

    private static void WriteBorder(int contentWidth)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine($"+{new string('-', contentWidth + 2)}+");
        Console.ForegroundColor = previousColor;
    }

    private static void WriteEmptyLine(int contentWidth)
        => Console.WriteLine($"| {new string(' ', contentWidth)} |");

    private static void WriteLine(int contentWidth, string text)
        => Console.WriteLine($"| {Pad(text, contentWidth)} |");

    private static void WriteAccentLine(string text)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(text);
        Console.ForegroundColor = previousColor;
    }

    private static void WriteKeyValueLine(int contentWidth, string label, string value)
        => WriteLine(contentWidth, $"{label,-7}{value}");

    private static void WriteWrappedText(int contentWidth, string text)
    {
        foreach (var line in Wrap(text, contentWidth))
        {
            WriteLine(contentWidth, line);
        }
    }

    private static IEnumerable<string> Wrap(string text, int width)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return string.Empty;
            yield break;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = string.Empty;

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(currentLine)
                ? word
                : $"{currentLine} {word}";

            if (candidate.Length <= width)
            {
                currentLine = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                yield return currentLine;
                currentLine = word;
                continue;
            }

            yield return word[..Math.Min(word.Length, width)];
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            yield return currentLine;
        }
    }

    private static string Pad(string text, int width)
        => text.Length >= width
            ? text[..width]
            : text.PadRight(width);
}
