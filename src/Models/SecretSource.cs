using System.IO;

namespace Models;

/// <summary>
/// Represents the logical source or service that a secret belongs to.
/// </summary>
public sealed class SecretSource : IEquatable<SecretSource>
{
    /// <summary>
    /// Initializes a validated secret source value.
    /// </summary>
    /// <param name="value">Raw source value.</param>
    public SecretSource(string? value)
    {
        Value = Normalize(value);
    }

    /// <summary>
    /// Gets the normalized source value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the normalized source value.
    /// </summary>
    /// <returns>The normalized source value.</returns>
    public override string ToString() => Value;

    /// <inheritdoc />
    public bool Equals(SecretSource? other)
        => other is not null &&
           string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SecretSource);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    private static string Normalize(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidDataException("Secret source is required.");
        }

        return normalized;
    }
}
