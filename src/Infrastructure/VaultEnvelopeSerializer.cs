using System.Text.Json;

namespace Infrastructure;

/// <summary>
/// Serializes and deserializes the encrypted vault envelope stored on disk.
/// </summary>
public static class VaultEnvelopeSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Serializes an encrypted vault envelope.
    /// </summary>
    /// <param name="document">Encrypted vault envelope.</param>
    /// <returns>The JSON representation.</returns>
    public static string Serialize(EncryptedVaultFileDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return JsonSerializer.Serialize(document, SerializerOptions);
    }

    /// <summary>
    /// Deserializes an encrypted vault envelope.
    /// </summary>
    /// <param name="json">JSON text to deserialize.</param>
    /// <returns>The encrypted vault envelope.</returns>
    public static EncryptedVaultFileDocument Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<EncryptedVaultFileDocument>(json, SerializerOptions)
                ?? throw new InvalidDataException("Vault file is invalid.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Vault file is invalid.", exception);
        }
    }
}
