using Models;

using System.Security.Cryptography;
using System.Text;

namespace Infrastructure;

/// <summary>
/// Encrypts and decrypts vault payloads.
/// </summary>
public static class VaultCryptographyService
{
    private const int CurrentVersion = 1;
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int SaltSize = 32;
    private const int TagSize = 16;
    private const int Pbkdf2Iterations = 600000;
    private const string MagicHeader = "LocalPass";

    /// <summary>
    /// Encrypts a vault into an encrypted envelope.
    /// </summary>
    /// <param name="vault">Vault to encrypt.</param>
    /// <param name="masterPassword">Master password used for encryption.</param>
    /// <returns>The encrypted vault envelope.</returns>
    public static EncryptedVaultFileDocument EncryptVault(SecretVault vault, MasterPassword masterPassword)
    {
        ArgumentNullException.ThrowIfNull(vault);
        ArgumentNullException.ThrowIfNull(masterPassword);

        var payload = VaultDocument.FromModel(vault);
        var payloadJson = VaultDocumentSerializer.Serialize(payload);
        var plaintext = Encoding.UTF8.GetBytes(payloadJson);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var key = DeriveKey(masterPassword, salt, Pbkdf2Iterations);
        var cipherText = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        var associatedData = BuildAssociatedData(Pbkdf2Iterations);

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintext, cipherText, tag, associatedData);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(key);
        }

        return new EncryptedVaultFileDocument
        {
            Version = CurrentVersion,
            Kdf = "PBKDF2-SHA512",
            Encryption = "AES-256-GCM",
            Iterations = Pbkdf2Iterations,
            Salt = Convert.ToBase64String(salt),
            Nonce = Convert.ToBase64String(nonce),
            Tag = Convert.ToBase64String(tag),
            CipherText = Convert.ToBase64String(cipherText)
        };
    }

    /// <summary>
    /// Decrypts an encrypted vault envelope into a vault model.
    /// </summary>
    /// <param name="document">Encrypted vault envelope.</param>
    /// <param name="masterPassword">Master password used for decryption.</param>
    /// <returns>The decrypted vault.</returns>
    public static SecretVault DecryptVault(EncryptedVaultFileDocument document, MasterPassword masterPassword)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(masterPassword);

        ValidateEnvelope(document);

        byte[] salt;
        byte[] nonce;
        byte[] tag;
        byte[] cipherText;

        try
        {
            salt = Convert.FromBase64String(document.Salt);
            nonce = Convert.FromBase64String(document.Nonce);
            tag = Convert.FromBase64String(document.Tag);
            cipherText = Convert.FromBase64String(document.CipherText);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException("Vault file is invalid.", exception);
        }

        var key = DeriveKey(masterPassword, salt, document.Iterations);
        var plaintext = new byte[cipherText.Length];
        var associatedData = BuildAssociatedData(document.Iterations);

        try
        {
            try
            {
                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(nonce, cipherText, tag, plaintext, associatedData);
            }
            catch (CryptographicException exception)
            {
                throw new InvalidDataException(
                    "The provided master password is incorrect or the vault file is corrupted.",
                    exception);
            }

            var payloadJson = Encoding.UTF8.GetString(plaintext);
            var payload = VaultDocumentSerializer.Deserialize(payloadJson);
            return payload.ToModel();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static void ValidateEnvelope(EncryptedVaultFileDocument document)
    {
        if (document.Version != CurrentVersion)
        {
            throw new InvalidDataException("Vault file version is not supported.");
        }

        if (!string.Equals(document.Kdf, "PBKDF2-SHA512", StringComparison.Ordinal) ||
            !string.Equals(document.Encryption, "AES-256-GCM", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(document.Salt) ||
            string.IsNullOrWhiteSpace(document.Nonce) ||
            string.IsNullOrWhiteSpace(document.Tag) ||
            string.IsNullOrWhiteSpace(document.CipherText) ||
            document.Iterations <= 0)
        {
            throw new InvalidDataException("Vault file is invalid.");
        }
    }

    private static byte[] BuildAssociatedData(int iterations)
        => Encoding.UTF8.GetBytes($"{MagicHeader}:{CurrentVersion}:{iterations}");

    private static byte[] DeriveKey(MasterPassword masterPassword, byte[] salt, int iterations)
        => Rfc2898DeriveBytes.Pbkdf2(
            masterPassword.Value,
            salt,
            iterations,
            HashAlgorithmName.SHA512,
            KeySize);
}
