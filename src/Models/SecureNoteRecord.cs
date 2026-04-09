using System.IO;

namespace Models;

/// <summary>
/// Represents a single immutable secure note stored in the vault.
/// </summary>
public sealed class SecureNoteRecord
{
    /// <summary>
    /// Initializes an immutable secure note record.
    /// </summary>
    /// <param name="id">Stable note identifier.</param>
    /// <param name="title">Validated note title.</param>
    /// <param name="description">Validated note description.</param>
    /// <param name="content">Validated note content.</param>
    /// <param name="createdUtc">Creation timestamp in UTC.</param>
    /// <param name="updatedUtc">Last update timestamp in UTC.</param>
    public SecureNoteRecord(
        Guid id,
        SecureNoteTitle title,
        SecureNoteDescription description,
        SecureNoteContent content,
        DateTimeOffset createdUtc,
        DateTimeOffset updatedUtc)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidDataException("Note identifier is required.");
        }

        Id = id;
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Content = content ?? throw new ArgumentNullException(nameof(content));
        CreatedUtc = EnsureUtc(createdUtc, "Created timestamp must be in UTC.");
        UpdatedUtc = EnsureUtc(updatedUtc, "Updated timestamp must be in UTC.");

        if (UpdatedUtc < CreatedUtc)
        {
            throw new InvalidDataException(
                "Updated timestamp must be on or after the created timestamp.");
        }
    }

    /// <summary>
    /// Gets the stable note identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the note title.
    /// </summary>
    public SecureNoteTitle Title { get; }

    /// <summary>
    /// Gets the note description.
    /// </summary>
    public SecureNoteDescription Description { get; }

    /// <summary>
    /// Gets the free-form note content.
    /// </summary>
    public SecureNoteContent Content { get; }

    /// <summary>
    /// Gets the creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; }

    /// <summary>
    /// Gets the last update timestamp in UTC.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; }

    /// <summary>
    /// Creates a new secure note record from raw input.
    /// </summary>
    /// <param name="title">Raw title value.</param>
    /// <param name="description">Raw description value.</param>
    /// <param name="content">Raw content value.</param>
    /// <param name="timestampUtc">Creation timestamp in UTC.</param>
    /// <returns>A validated secure note record.</returns>
    public static SecureNoteRecord Create(
        string? title,
        string? description,
        string? content,
        DateTimeOffset timestampUtc)
        => new(
            Guid.NewGuid(),
            new SecureNoteTitle(title),
            new SecureNoteDescription(description),
            new SecureNoteContent(content),
            EnsureUtc(timestampUtc, "Created timestamp must be in UTC."),
            EnsureUtc(timestampUtc, "Updated timestamp must be in UTC."));

    /// <summary>
    /// Creates an updated immutable copy of the current note record.
    /// </summary>
    /// <param name="title">Raw title value.</param>
    /// <param name="description">Raw description value.</param>
    /// <param name="content">Raw content value.</param>
    /// <param name="updatedUtc">Update timestamp in UTC.</param>
    /// <returns>An updated immutable secure note record.</returns>
    public SecureNoteRecord Update(
        string? title,
        string? description,
        string? content,
        DateTimeOffset updatedUtc)
        => new(
            Id,
            new SecureNoteTitle(title),
            new SecureNoteDescription(description),
            new SecureNoteContent(content),
            CreatedUtc,
            EnsureUtc(updatedUtc, "Updated timestamp must be in UTC."));

    private static DateTimeOffset EnsureUtc(DateTimeOffset value, string message)
        => value.Offset == TimeSpan.Zero
            ? value
            : throw new InvalidDataException(message);
}
