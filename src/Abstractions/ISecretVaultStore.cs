using Models;

namespace Abstractions;

/// <summary>
/// Persists encrypted secret vault data on local storage.
/// </summary>
public interface ISecretVaultStore
{
    /// <summary>
    /// Determines whether the encrypted vault file already exists.
    /// </summary>
    /// <returns><see langword="true"/> when the vault file exists; otherwise, <see langword="false"/>.</returns>
    bool Exists();

    /// <summary>
    /// Creates a brand-new encrypted vault by using the provided master password.
    /// </summary>
    /// <param name="masterPassword">Validated master password used for encryption.</param>
    /// <returns>The newly created unlocked session.</returns>
    SecretVaultSession CreateNew(MasterPassword masterPassword);

    /// <summary>
    /// Opens an existing encrypted vault by using the provided master password.
    /// </summary>
    /// <param name="masterPassword">Validated master password used for decryption.</param>
    /// <returns>The unlocked vault session.</returns>
    SecretVaultSession Open(MasterPassword masterPassword);

    /// <summary>
    /// Saves the supplied vault session to the encrypted vault file.
    /// </summary>
    /// <param name="session">Unlocked session to persist.</param>
    /// <returns>The saved session.</returns>
    SecretVaultSession Save(SecretVaultSession session);

    /// <summary>
    /// Re-encrypts the vault by using a new master password.
    /// </summary>
    /// <param name="session">Existing unlocked session.</param>
    /// <param name="newMasterPassword">New validated master password.</param>
    /// <returns>The updated session encrypted with the new master password.</returns>
    SecretVaultSession ChangeMasterPassword(
        SecretVaultSession session,
        MasterPassword newMasterPassword);
}
