namespace Models;

/// <summary>
/// Represents optional notes associated with a secret.
/// </summary>
public sealed class SecretNotes : IEquatable<SecretNotes>
{
    /// <summary>
    /// Initializes optional notes for a secret entry.
    /// </summary>
    /// <param name="value">Raw notes value.</param>
    public SecretNotes(string? value)
    {
        Value = value?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Gets the normalized notes value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets a value indicating whether notes are present.
    /// </summary>
    public bool HasValue => Value.Length > 0;

    /// <summary>
    /// Returns the normalized notes value.
    /// </summary>
    /// <returns>The normalized notes value.</returns>
    public override string ToString() => Value;

    /// <inheritdoc />
    public bool Equals(SecretNotes? other)
        => other is not null &&
           string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SecretNotes);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
}
