using Models;

using System.Text.Json;

namespace Infrastructure;

/// <summary>
/// Serializes and deserializes the plaintext vault payload document.
/// </summary>
public static class VaultDocumentSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Serializes a vault payload document.
    /// </summary>
    /// <param name="document">Vault payload document.</param>
    /// <returns>The JSON representation.</returns>
    public static string Serialize(VaultDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return JsonSerializer.Serialize(document, SerializerOptions);
    }

    /// <summary>
    /// Deserializes a vault payload document.
    /// </summary>
    /// <param name="json">JSON text to deserialize.</param>
    /// <returns>The payload document.</returns>
    public static VaultDocument Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<VaultDocument>(json, SerializerOptions)
                ?? throw new InvalidDataException("Vault payload is invalid.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Vault payload is invalid.", exception);
        }
    }
}
