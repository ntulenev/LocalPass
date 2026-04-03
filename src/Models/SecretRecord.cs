using System.IO;

namespace Models;

/// <summary>
/// Represents a single immutable secret stored in the vault.
/// </summary>
public sealed class SecretRecord
{
    /// <summary>
    /// Initializes an immutable secret record.
    /// </summary>
    /// <param name="id">Stable secret identifier.</param>
    /// <param name="source">Validated source or service name.</param>
    /// <param name="login">Validated login or user identifier.</param>
    /// <param name="password">Validated secret password.</param>
    /// <param name="notes">Optional notes.</param>
    /// <param name="createdUtc">Creation timestamp in UTC.</param>
    /// <param name="updatedUtc">Last update timestamp in UTC.</param>
    public SecretRecord(
        Guid id,
        SecretSource source,
        SecretLogin login,
        SecretPassword password,
        SecretNotes? notes,
        DateTimeOffset createdUtc,
        DateTimeOffset updatedUtc)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidDataException("Secret identifier is required.");
        }

        Id = id;
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Login = login ?? throw new ArgumentNullException(nameof(login));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        Notes = notes ?? new SecretNotes(null);
        CreatedUtc = EnsureUtc(createdUtc, "Created timestamp must be in UTC.");
        UpdatedUtc = EnsureUtc(updatedUtc, "Updated timestamp must be in UTC.");

        if (UpdatedUtc < CreatedUtc)
        {
            throw new InvalidDataException(
                "Updated timestamp must be on or after the created timestamp.");
        }
    }

    /// <summary>
    /// Gets the stable secret identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the secret source or service name.
    /// </summary>
    public SecretSource Source { get; }

    /// <summary>
    /// Gets the secret login or user identifier.
    /// </summary>
    public SecretLogin Login { get; }

    /// <summary>
    /// Gets the secret password value object.
    /// </summary>
    public SecretPassword Password { get; }

    /// <summary>
    /// Gets the optional notes.
    /// </summary>
    public SecretNotes Notes { get; }

    /// <summary>
    /// Gets the creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; }

    /// <summary>
    /// Gets the last update timestamp in UTC.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; }

    /// <summary>
    /// Creates a new secret record from raw input.
    /// </summary>
    /// <param name="source">Raw source or service name.</param>
    /// <param name="login">Raw login value.</param>
    /// <param name="password">Raw password value.</param>
    /// <param name="notes">Raw notes value.</param>
    /// <param name="timestampUtc">Creation timestamp in UTC.</param>
    /// <returns>A validated secret record.</returns>
    public static SecretRecord Create(
        string? source,
        string? login,
        string? password,
        string? notes,
        DateTimeOffset timestampUtc)
        => new(
            Guid.NewGuid(),
            new SecretSource(source),
            new SecretLogin(login),
            new SecretPassword(password),
            new SecretNotes(notes),
            EnsureUtc(timestampUtc, "Created timestamp must be in UTC."),
            EnsureUtc(timestampUtc, "Updated timestamp must be in UTC."));

    /// <summary>
    /// Creates an updated immutable copy of the current secret record.
    /// </summary>
    /// <param name="source">Raw source or service name.</param>
    /// <param name="login">Raw login value.</param>
    /// <param name="password">Raw password value.</param>
    /// <param name="notes">Raw notes value.</param>
    /// <param name="updatedUtc">Update timestamp in UTC.</param>
    /// <returns>An updated immutable secret record.</returns>
    public SecretRecord Update(
        string? source,
        string? login,
        string? password,
        string? notes,
        DateTimeOffset updatedUtc)
        => new(
            Id,
            new SecretSource(source),
            new SecretLogin(login),
            new SecretPassword(password),
            new SecretNotes(notes),
            CreatedUtc,
            EnsureUtc(updatedUtc, "Updated timestamp must be in UTC."));

    private static DateTimeOffset EnsureUtc(DateTimeOffset value, string message)
        => value.Offset == TimeSpan.Zero
            ? value
            : throw new InvalidDataException(message);
}
