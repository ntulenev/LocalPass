using System.Collections.ObjectModel;
using System.IO;

namespace Models;

/// <summary>
/// Represents the immutable secret vault aggregate.
/// </summary>
public sealed class SecretVault
{
    /// <summary>
    /// Initializes an immutable secret vault.
    /// </summary>
    /// <param name="entries">Secret entries stored in the vault.</param>
    /// <param name="notes">Secure notes stored in the vault.</param>
    /// <param name="createdUtc">Creation timestamp in UTC.</param>
    /// <param name="updatedUtc">Last update timestamp in UTC.</param>
    /// <param name="documentVersion">User-visible vault document revision.</param>
    public SecretVault(
        IEnumerable<SecretRecord> entries,
        IEnumerable<SecureNoteRecord> notes,
        DateTimeOffset createdUtc,
        DateTimeOffset updatedUtc,
        int documentVersion = 1)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(notes);

        CreatedUtc = EnsureUtc(createdUtc, "Vault created timestamp must be in UTC.");
        UpdatedUtc = EnsureUtc(updatedUtc, "Vault updated timestamp must be in UTC.");
        DocumentVersion = documentVersion > 0
            ? documentVersion
            : throw new InvalidDataException("Vault document version must be greater than zero.");
        if (UpdatedUtc < CreatedUtc)
        {
            throw new InvalidDataException(
                "Vault updated timestamp must be on or after the created timestamp.");
        }

        Entries = new ReadOnlyCollection<SecretRecord>([
            .. entries
                .OrderBy(entry => entry.Source.Value, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Login.Value, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Id)
        ]);
        Notes = new ReadOnlyCollection<SecureNoteRecord>([
            .. notes
                .OrderBy(note => note.Title.Value, StringComparer.OrdinalIgnoreCase)
                .ThenBy(note => note.Description.Value, StringComparer.OrdinalIgnoreCase)
                .ThenBy(note => note.Id)
        ]);
    }

    /// <summary>
    /// Initializes an immutable secret vault.
    /// </summary>
    /// <param name="entries">Secret entries stored in the vault.</param>
    /// <param name="createdUtc">Creation timestamp in UTC.</param>
    /// <param name="updatedUtc">Last update timestamp in UTC.</param>
    /// <param name="documentVersion">User-visible vault document revision.</param>
    public SecretVault(
        IEnumerable<SecretRecord> entries,
        DateTimeOffset createdUtc,
        DateTimeOffset updatedUtc,
        int documentVersion = 1)
        : this(entries, [], createdUtc, updatedUtc, documentVersion)
    {
    }

    /// <summary>
    /// Gets the vault creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; }

    /// <summary>
    /// Gets the last vault update timestamp in UTC.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; }

    /// <summary>
    /// Gets the user-visible vault document revision.
    /// </summary>
    public int DocumentVersion { get; }

    /// <summary>
    /// Gets the immutable vault entries.
    /// </summary>
    public ReadOnlyCollection<SecretRecord> Entries { get; }

    /// <summary>
    /// Gets the immutable vault notes.
    /// </summary>
    public ReadOnlyCollection<SecureNoteRecord> Notes { get; }

    /// <summary>
    /// Gets the number of stored secrets.
    /// </summary>
    public int Count => Entries.Count;

    /// <summary>
    /// Gets the number of stored secure notes.
    /// </summary>
    public int NoteCount => Notes.Count;

    /// <summary>
    /// Gets a value indicating whether the vault contains secrets.
    /// </summary>
    public bool HasEntries => Count > 0;

    /// <summary>
    /// Gets a value indicating whether the vault contains secure notes.
    /// </summary>
    public bool HasNotes => NoteCount > 0;

    /// <summary>
    /// Creates an empty vault.
    /// </summary>
    /// <param name="timestampUtc">Creation timestamp in UTC.</param>
    /// <returns>An empty immutable vault.</returns>
    public static SecretVault CreateEmpty(DateTimeOffset timestampUtc)
        => new([], [], EnsureUtc(timestampUtc, "Vault created timestamp must be in UTC."), timestampUtc, 1);

    /// <summary>
    /// Gets the secret at the supplied index.
    /// </summary>
    /// <param name="index">Zero-based secret index.</param>
    /// <returns>The requested secret record.</returns>
    public SecretRecord GetSecret(int index) => Entries[index];

    /// <summary>
    /// Returns the index of a secret identifier, or <c>-1</c> when it is not present.
    /// </summary>
    /// <param name="id">Secret identifier to locate.</param>
    /// <returns>The zero-based index, or <c>-1</c> when not found.</returns>
    public int FindIndex(Guid id)
        => Entries
            .Select((entry, index) => new { entry.Id, Index = index })
            .FirstOrDefault(item => item.Id == id)
            ?.Index ?? -1;

    /// <summary>
    /// Gets the secure note at the supplied index.
    /// </summary>
    /// <param name="index">Zero-based secure note index.</param>
    /// <returns>The requested secure note record.</returns>
    public SecureNoteRecord GetNote(int index) => Notes[index];

    /// <summary>
    /// Returns the index of a secure note identifier, or <c>-1</c> when it is not present.
    /// </summary>
    /// <param name="id">Secure note identifier to locate.</param>
    /// <returns>The zero-based index, or <c>-1</c> when not found.</returns>
    public int FindNoteIndex(Guid id)
        => Notes
            .Select((note, index) => new { note.Id, Index = index })
            .FirstOrDefault(item => item.Id == id)
            ?.Index ?? -1;

    /// <summary>
    /// Adds a new secret or replaces an existing secret with the same identifier.
    /// </summary>
    /// <param name="record">Secret record to add or replace.</param>
    /// <param name="updatedUtc">Update timestamp in UTC.</param>
    /// <returns>A new vault snapshot with the supplied record.</returns>
    public SecretVault WithEntry(SecretRecord record, DateTimeOffset updatedUtc)
    {
        ArgumentNullException.ThrowIfNull(record);

        var normalizedUpdatedUtc = EnsureUtc(updatedUtc, "Vault updated timestamp must be in UTC.");
        var remainingEntries = Entries.Where(existing => existing.Id != record.Id);
        return new SecretVault(
            [.. remainingEntries, record],
            Notes,
            CreatedUtc,
            normalizedUpdatedUtc,
            GetNextDocumentVersion());
    }

    /// <summary>
    /// Removes a secret from the vault.
    /// </summary>
    /// <param name="id">Identifier of the secret to remove.</param>
    /// <param name="updatedUtc">Update timestamp in UTC.</param>
    /// <returns>A new vault snapshot without the supplied secret.</returns>
    public SecretVault WithoutEntry(Guid id, DateTimeOffset updatedUtc)
    {
        var remainingEntries = Entries.Where(entry => entry.Id != id).ToArray();
        if (remainingEntries.Length == Entries.Count)
        {
            throw new InvalidDataException("Secret record was not found.");
        }

        return new SecretVault(
            remainingEntries,
            Notes,
            CreatedUtc,
            EnsureUtc(updatedUtc, "Vault updated timestamp must be in UTC."),
            GetNextDocumentVersion());
    }

    /// <summary>
    /// Adds a new secure note or replaces an existing note with the same identifier.
    /// </summary>
    /// <param name="note">Secure note to add or replace.</param>
    /// <param name="updatedUtc">Update timestamp in UTC.</param>
    /// <returns>A new vault snapshot with the supplied note.</returns>
    public SecretVault WithNote(SecureNoteRecord note, DateTimeOffset updatedUtc)
    {
        ArgumentNullException.ThrowIfNull(note);

        var normalizedUpdatedUtc = EnsureUtc(updatedUtc, "Vault updated timestamp must be in UTC.");
        var remainingNotes = Notes.Where(existing => existing.Id != note.Id);
        return new SecretVault(
            Entries,
            [.. remainingNotes, note],
            CreatedUtc,
            normalizedUpdatedUtc,
            GetNextDocumentVersion());
    }

    /// <summary>
    /// Removes a secure note from the vault.
    /// </summary>
    /// <param name="id">Identifier of the secure note to remove.</param>
    /// <param name="updatedUtc">Update timestamp in UTC.</param>
    /// <returns>A new vault snapshot without the supplied note.</returns>
    public SecretVault WithoutNote(Guid id, DateTimeOffset updatedUtc)
    {
        var remainingNotes = Notes.Where(note => note.Id != id).ToArray();
        if (remainingNotes.Length == Notes.Count)
        {
            throw new InvalidDataException("Secure note was not found.");
        }

        return new SecretVault(
            Entries,
            remainingNotes,
            CreatedUtc,
            EnsureUtc(updatedUtc, "Vault updated timestamp must be in UTC."),
            GetNextDocumentVersion());
    }

    private int GetNextDocumentVersion() => checked(DocumentVersion + 1);

    private static DateTimeOffset EnsureUtc(DateTimeOffset value, string message)
        => value.Offset == TimeSpan.Zero
            ? value
            : throw new InvalidDataException(message);
}
