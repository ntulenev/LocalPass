using System.IO;

namespace Models;

/// <summary>
/// Represents the validated title of a secure note.
/// </summary>
public sealed class SecureNoteTitle : IEquatable<SecureNoteTitle>
{
    /// <summary>
    /// Initializes a validated note title.
    /// </summary>
    /// <param name="value">Raw title value.</param>
    public SecureNoteTitle(string? value)
    {
        Value = Normalize(value);
    }

    /// <summary>
    /// Gets the normalized title value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the normalized title value.
    /// </summary>
    /// <returns>The normalized title value.</returns>
    public override string ToString() => Value;

    /// <inheritdoc />
    public bool Equals(SecureNoteTitle? other)
        => other is not null &&
           string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SecureNoteTitle);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    private static string Normalize(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidDataException("Note title is required.");
        }

        return normalized;
    }
}
