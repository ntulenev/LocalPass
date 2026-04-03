using System.IO;

namespace Models;

/// <summary>
/// Represents the login or user identifier associated with a secret.
/// </summary>
public sealed class SecretLogin : IEquatable<SecretLogin>
{
    /// <summary>
    /// Initializes a validated secret login value.
    /// </summary>
    /// <param name="value">Raw login value.</param>
    public SecretLogin(string? value)
    {
        Value = Normalize(value);
    }

    /// <summary>
    /// Gets the normalized login value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the normalized login value.
    /// </summary>
    /// <returns>The normalized login value.</returns>
    public override string ToString() => Value;

    /// <inheritdoc />
    public bool Equals(SecretLogin? other)
        => other is not null &&
           string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SecretLogin);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    private static string Normalize(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidDataException("Secret login is required.");
        }

        return normalized;
    }
}
