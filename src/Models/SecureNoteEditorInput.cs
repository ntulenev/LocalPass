namespace Models;

/// <summary>
/// Represents validated user input used to create or update a secure note.
/// </summary>
public sealed class SecureNoteEditorInput
{
    /// <summary>
    /// Initializes validated secure note editor input.
    /// </summary>
    /// <param name="title">Validated note title.</param>
    /// <param name="description">Validated note description.</param>
    /// <param name="content">Validated note content.</param>
    public SecureNoteEditorInput(
        SecureNoteTitle title,
        SecureNoteDescription description,
        SecureNoteContent content)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    /// <summary>
    /// Gets the validated note title.
    /// </summary>
    public SecureNoteTitle Title { get; }

    /// <summary>
    /// Gets the validated note description.
    /// </summary>
    public SecureNoteDescription Description { get; }

    /// <summary>
    /// Gets the validated note content.
    /// </summary>
    public SecureNoteContent Content { get; }

    /// <summary>
    /// Creates validated input from raw editor field values.
    /// </summary>
    /// <param name="title">Raw title value.</param>
    /// <param name="description">Raw description value.</param>
    /// <param name="content">Raw content value.</param>
    /// <returns>Validated input model.</returns>
    public static SecureNoteEditorInput Create(
        string? title,
        string? description,
        string? content)
        => new(
            new SecureNoteTitle(title),
            new SecureNoteDescription(description),
            new SecureNoteContent(content));

    /// <summary>
    /// Creates a new immutable secure note record from this validated input.
    /// </summary>
    /// <param name="timestampUtc">Creation timestamp in UTC.</param>
    /// <returns>A new secure note record.</returns>
    public SecureNoteRecord ToRecord(DateTimeOffset timestampUtc)
        => SecureNoteRecord.Create(
            Title.Value,
            Description.Value,
            Content.Value,
            timestampUtc);

    /// <summary>
    /// Applies this validated input to an existing immutable secure note record.
    /// </summary>
    /// <param name="note">Existing note to update.</param>
    /// <param name="updatedUtc">Update timestamp in UTC.</param>
    /// <returns>The updated secure note record.</returns>
    public SecureNoteRecord ApplyTo(SecureNoteRecord note, DateTimeOffset updatedUtc)
    {
        ArgumentNullException.ThrowIfNull(note);

        return note.Update(
            Title.Value,
            Description.Value,
            Content.Value,
            updatedUtc);
    }
}
