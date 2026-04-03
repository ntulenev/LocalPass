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
    /// <param name="createdUtc">Creation timestamp in UTC.</param>
    /// <param name="updatedUtc">Last update timestamp in UTC.</param>
    public SecretVault(
        IEnumerable<SecretRecord> entries,
        DateTimeOffset createdUtc,
        DateTimeOffset updatedUtc)
    {
        ArgumentNullException.ThrowIfNull(entries);

        CreatedUtc = EnsureUtc(createdUtc, "Vault created timestamp must be in UTC.");
        UpdatedUtc = EnsureUtc(updatedUtc, "Vault updated timestamp must be in UTC.");
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
    /// Gets the immutable vault entries.
    /// </summary>
    public ReadOnlyCollection<SecretRecord> Entries { get; }

    /// <summary>
    /// Gets the number of stored secrets.
    /// </summary>
    public int Count => Entries.Count;

    /// <summary>
    /// Gets a value indicating whether the vault contains secrets.
    /// </summary>
    public bool HasEntries => Count > 0;

    /// <summary>
    /// Creates an empty vault.
    /// </summary>
    /// <param name="timestampUtc">Creation timestamp in UTC.</param>
    /// <returns>An empty immutable vault.</returns>
    public static SecretVault CreateEmpty(DateTimeOffset timestampUtc)
        => new([], EnsureUtc(timestampUtc, "Vault created timestamp must be in UTC."), timestampUtc);

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
        return new SecretVault([.. remainingEntries, record], CreatedUtc, normalizedUpdatedUtc);
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
            CreatedUtc,
            EnsureUtc(updatedUtc, "Vault updated timestamp must be in UTC."));
    }

    private static DateTimeOffset EnsureUtc(DateTimeOffset value, string message)
        => value.Offset == TimeSpan.Zero
            ? value
            : throw new InvalidDataException(message);
}
