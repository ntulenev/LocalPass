using System.IO;

namespace Models;

/// <summary>
/// Represents the free-form body text of a secure note.
/// </summary>
public sealed class SecureNoteContent : IEquatable<SecureNoteContent>
{
    /// <summary>
    /// Initializes validated note content.
    /// </summary>
    /// <param name="value">Raw content value.</param>
    public SecureNoteContent(string? value)
    {
        Value = Normalize(value);
    }

    /// <summary>
    /// Gets the raw content value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the raw content value.
    /// </summary>
    /// <returns>The raw content value.</returns>
    public override string ToString() => Value;

    /// <inheritdoc />
    public bool Equals(SecureNoteContent? other)
        => other is not null &&
           string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SecureNoteContent);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException("Note content is required.");
        }

        return value;
    }
}
