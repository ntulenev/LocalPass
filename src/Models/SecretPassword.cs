using System.IO;

namespace Models;

/// <summary>
/// Represents the password or secret value stored in the vault.
/// </summary>
public sealed class SecretPassword : IEquatable<SecretPassword>
{
    /// <summary>
    /// Initializes a validated secret password.
    /// </summary>
    /// <param name="value">Raw secret password value.</param>
    public SecretPassword(string? value)
    {
        Value = Normalize(value);
    }

    /// <summary>
    /// Gets the raw password value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns a redacted display value.
    /// </summary>
    /// <returns>A redacted string.</returns>
    public override string ToString() => "[REDACTED]";

    /// <inheritdoc />
    public bool Equals(SecretPassword? other)
        => other is not null &&
           string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SecretPassword);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    private static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.All(char.IsWhiteSpace))
        {
            throw new InvalidDataException("Secret password is required.");
        }

        return value;
    }
}
