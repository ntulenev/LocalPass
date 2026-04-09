using System.IO;

namespace Models;

/// <summary>
/// Represents the validated short description of a secure note.
/// </summary>
public sealed class SecureNoteDescription : IEquatable<SecureNoteDescription>
{
    /// <summary>
    /// Initializes a validated note description.
    /// </summary>
    /// <param name="value">Raw description value.</param>
    public SecureNoteDescription(string? value)
    {
        Value = Normalize(value);
    }

    /// <summary>
    /// Gets the normalized description value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the normalized description value.
    /// </summary>
    /// <returns>The normalized description value.</returns>
    public override string ToString() => Value;

    /// <inheritdoc />
    public bool Equals(SecureNoteDescription? other)
        => other is not null &&
           string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as SecureNoteDescription);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    private static string Normalize(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidDataException("Note description is required.");
        }

        return normalized;
    }
}
