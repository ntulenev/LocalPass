using Models;

namespace Infrastructure;

/// <summary>
/// Mutable storage document for vault contents before encryption.
/// </summary>
public sealed class VaultDocument
{
    /// <summary>
    /// Gets or sets the vault creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; }

    /// <summary>
    /// Gets or sets the vault update timestamp in UTC.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; }

    /// <summary>
    /// Gets or sets the user-visible vault document revision.
    /// </summary>
    public int DocumentVersion { get; set; } = 1;

    /// <summary>
    /// Gets or sets the stored secret entries.
    /// </summary>
    public List<SecretRecordDocument> Entries { get; set; } = [];

    /// <summary>
    /// Creates a mutable storage document from an immutable vault.
    /// </summary>
    /// <param name="vault">Vault model to map.</param>
    /// <returns>A mutable storage document.</returns>
    public static VaultDocument FromModel(SecretVault vault)
    {
        ArgumentNullException.ThrowIfNull(vault);

        return new VaultDocument
        {
            CreatedUtc = vault.CreatedUtc,
            UpdatedUtc = vault.UpdatedUtc,
            DocumentVersion = vault.DocumentVersion,
            Entries = [.. vault.Entries.Select(SecretRecordDocument.FromModel)]
        };
    }

    /// <summary>
    /// Converts the storage document into an immutable vault model.
    /// </summary>
    /// <returns>A validated immutable vault.</returns>
    public SecretVault ToModel()
        => new(
            Entries.Select(entry => entry.ToModel()),
            CreatedUtc,
            UpdatedUtc,
            DocumentVersion);
}
