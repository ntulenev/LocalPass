using Abstractions;

using Models;

using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Infrastructure;

/// <summary>
/// Stores the LocalPass vault in a locally encrypted file.
/// </summary>
public sealed class FileSecretVaultStore : ISecretVaultStore
{
    private const int CurrentVersion = 1;
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int SaltSize = 32;
    private const int TagSize = 16;
    private const int Pbkdf2Iterations = 600000;
    private const string VaultFileName = "vault.localpass";
    private const string SnapshotDirectoryName = "snapshots";
    private const string MagicHeader = "LocalPass";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a store that uses the default LocalPass storage location.
    /// </summary>
    /// <param name="clock">Clock used for vault timestamps and snapshot naming.</param>
    public FileSecretVaultStore(IClock clock)
        : this(GetDefaultStorageDirectory(), clock)
    {
    }

    /// <summary>
    /// Initializes a store that writes to a specific storage directory.
    /// </summary>
    /// <param name="storageDirectory">Directory containing the encrypted vault and snapshots.</param>
    /// <param name="clock">Clock used for vault timestamps and snapshot naming.</param>
    public FileSecretVaultStore(string storageDirectory, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(storageDirectory))
        {
            throw new ArgumentException("Storage directory is required.", nameof(storageDirectory));
        }

        _storageDirectory = Path.GetFullPath(storageDirectory);
        _vaultFilePath = Path.Combine(_storageDirectory, VaultFileName);
        _snapshotDirectoryPath = Path.Combine(_storageDirectory, SnapshotDirectoryName);
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc />
    public bool Exists() => File.Exists(_vaultFilePath);

    /// <inheritdoc />
    public SecretVaultSession CreateNew(MasterPassword masterPassword)
    {
        ArgumentNullException.ThrowIfNull(masterPassword);

        var vault = SecretVault.CreateEmpty(_clock.UtcNow);
        return Save(new SecretVaultSession(vault, masterPassword));
    }

    /// <inheritdoc />
    public SecretVaultSession Open(MasterPassword masterPassword)
    {
        ArgumentNullException.ThrowIfNull(masterPassword);

        if (!File.Exists(_vaultFilePath))
        {
            throw new FileNotFoundException("Vault file was not found.", _vaultFilePath);
        }

        var json = File.ReadAllText(_vaultFilePath, Encoding.UTF8);
        EncryptedVaultFileDocument document;
        try
        {
            document = JsonSerializer.Deserialize<EncryptedVaultFileDocument>(json, SerializerOptions)
                ?? throw new InvalidDataException("Vault file is invalid.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Vault file is invalid.", exception);
        }

        var vault = DecryptVault(document, masterPassword);
        return new SecretVaultSession(vault, masterPassword);
    }

    /// <inheritdoc />
    public SecretVaultSession Save(SecretVaultSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        Directory.CreateDirectory(_storageDirectory);
        Directory.CreateDirectory(_snapshotDirectoryPath);

        var document = EncryptVault(session.Vault, session.MasterPassword);
        var json = JsonSerializer.Serialize(document, SerializerOptions);
        var temporaryPath = Path.Combine(_storageDirectory, $"{Guid.NewGuid():N}.tmp");

        try
        {
            WriteAllText(temporaryPath, json);

            if (File.Exists(_vaultFilePath))
            {
                var snapshotPath = BuildSnapshotPath();
                File.Replace(temporaryPath, _vaultFilePath, snapshotPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, _vaultFilePath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return session;
    }

    /// <inheritdoc />
    public SecretVaultSession ChangeMasterPassword(
        SecretVaultSession session,
        MasterPassword newMasterPassword)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(newMasterPassword);

        return Save(session.WithMasterPassword(newMasterPassword));
    }

    private static EncryptedVaultFileDocument EncryptVault(
        SecretVault vault,
        MasterPassword masterPassword)
    {
        ArgumentNullException.ThrowIfNull(vault);
        ArgumentNullException.ThrowIfNull(masterPassword);

        var payload = VaultDocument.FromModel(vault);
        var payloadJson = JsonSerializer.Serialize(payload, SerializerOptions);
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

    private static SecretVault DecryptVault(
        EncryptedVaultFileDocument document,
        MasterPassword masterPassword)
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
            VaultDocument payload;
            try
            {
                payload = JsonSerializer.Deserialize<VaultDocument>(payloadJson, SerializerOptions)
                    ?? throw new InvalidDataException("Vault payload is invalid.");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("Vault payload is invalid.", exception);
            }

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

    private static string GetDefaultStorageDirectory()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LocalPass");

    private static void WriteAllText(string path, string content)
    {
        using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.WriteThrough);
        using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
        writer.Flush();
        stream.Flush(flushToDisk: true);
    }

    private string BuildSnapshotPath()
    {
        var timestamp = _clock.UtcNow.ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture);
        return Path.Combine(_snapshotDirectoryPath, $"vault-{timestamp}-{Guid.NewGuid():N}.localpass");
    }

    private readonly IClock _clock;
    private readonly string _snapshotDirectoryPath;
    private readonly string _storageDirectory;
    private readonly string _vaultFilePath;
}
