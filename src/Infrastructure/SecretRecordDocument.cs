using Models;

namespace Infrastructure;

/// <summary>
/// Mutable storage document for a secret record.
/// </summary>
public sealed class SecretRecordDocument
{
    /// <summary>
    /// Gets or sets the secret identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the secret source or service name.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the login or user identifier.
    /// </summary>
    public string? Login { get; set; }

    /// <summary>
    /// Gets or sets the raw secret password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets optional notes.
    /// </summary>
    public string? Notes { get; set; }

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
    /// <param name="record">Source secret record.</param>
    /// <returns>A mutable storage document.</returns>
    public static SecretRecordDocument FromModel(SecretRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new SecretRecordDocument
        {
            Id = record.Id,
            Source = record.Source.Value,
            Login = record.Login.Value,
            Password = record.Password.Value,
            Notes = record.Notes.Value,
            CreatedUtc = record.CreatedUtc,
            UpdatedUtc = record.UpdatedUtc
        };
    }

    /// <summary>
    /// Converts the storage document into an immutable model.
    /// </summary>
    /// <returns>A validated immutable secret record.</returns>
    public SecretRecord ToModel()
        => new(
            Id,
            new SecretSource(Source),
            new SecretLogin(Login),
            new SecretPassword(Password),
            new SecretNotes(Notes),
            CreatedUtc,
            UpdatedUtc);
}
