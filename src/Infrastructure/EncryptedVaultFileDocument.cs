namespace Infrastructure;

/// <summary>
/// Represents the encrypted vault envelope stored on disk.
/// </summary>
public sealed class EncryptedVaultFileDocument
{
    /// <summary>
    /// Gets or sets the envelope format version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets the key derivation function name.
    /// </summary>
    public string Kdf { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encryption algorithm name.
    /// </summary>
    public string Encryption { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the PBKDF2 iteration count.
    /// </summary>
    public int Iterations { get; set; }

    /// <summary>
    /// Gets or sets the base64-encoded salt.
    /// </summary>
    public string Salt { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base64-encoded nonce.
    /// </summary>
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base64-encoded authentication tag.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base64-encoded ciphertext payload.
    /// </summary>
    public string CipherText { get; set; } = string.Empty;
}
