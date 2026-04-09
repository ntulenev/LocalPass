using Models;

namespace Infrastructure;

/// <summary>
/// Mutable storage document for a secure note record.
/// </summary>
public sealed class SecureNoteRecordDocument
{
    /// <summary>
    /// Gets or sets the note identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the note title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the note description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the raw secure note content.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; }

    /// <summary>
    /// Creates a mutable document from an immutable model.
    /// </summary>
    /// <param name="record">Source secure note record.</param>
    /// <returns>A mutable storage document.</returns>
    public static SecureNoteRecordDocument FromModel(SecureNoteRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new SecureNoteRecordDocument
        {
            Id = record.Id,
            Title = record.Title.Value,
            Description = record.Description.Value,
            Content = record.Content.Value,
            CreatedUtc = record.CreatedUtc,
            UpdatedUtc = record.UpdatedUtc
        };
    }

    /// <summary>
    /// Converts the storage document into an immutable model.
    /// </summary>
    /// <returns>A validated immutable secure note record.</returns>
    public SecureNoteRecord ToModel()
        => new(
            Id,
            new SecureNoteTitle(Title),
            new SecureNoteDescription(Description),
            new SecureNoteContent(Content),
            CreatedUtc,
            UpdatedUtc);
}
