using System.IO;

namespace Models;

/// <summary>
/// Represents a validated master password used to protect the vault file.
/// </summary>
public sealed class MasterPassword
{
    /// <summary>
    /// Initializes a validated master password.
    /// </summary>
    /// <param name="value">Raw master password value.</param>
    public MasterPassword(string? value)
    {
        Value = Normalize(value);
    }

    /// <summary>
    /// Gets the validated password value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns a redacted display value.
    /// </summary>
    /// <returns>A redacted string.</returns>
    public override string ToString() => "[REDACTED]";

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException("Master password is required.");
        }

        if (value.Any(char.IsWhiteSpace))
        {
            throw new InvalidDataException("Master password must not contain whitespace.");
        }

        if (value.Length < 16)
        {
            throw new InvalidDataException("Master password must be at least 16 characters long.");
        }

        if (!value.Any(char.IsUpper))
        {
            throw new InvalidDataException("Master password must contain an uppercase letter.");
        }

        if (!value.Any(char.IsLower))
        {
            throw new InvalidDataException("Master password must contain a lowercase letter.");
        }

        if (!value.Any(char.IsDigit))
        {
            throw new InvalidDataException("Master password must contain a digit.");
        }

        if (!value.Any(static character => char.IsPunctuation(character) || char.IsSymbol(character)))
        {
            throw new InvalidDataException("Master password must contain a symbol.");
        }

        return value;
    }
}
