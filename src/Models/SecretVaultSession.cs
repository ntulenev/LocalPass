namespace Models;

/// <summary>
/// Represents an unlocked vault together with the master password used for persistence.
/// </summary>
public sealed class SecretVaultSession
{
    /// <summary>
    /// Initializes an unlocked vault session.
    /// </summary>
    /// <param name="vault">Unlocked vault snapshot.</param>
    /// <param name="masterPassword">Validated master password.</param>
    public SecretVaultSession(SecretVault vault, MasterPassword masterPassword)
    {
        Vault = vault ?? throw new ArgumentNullException(nameof(vault));
        MasterPassword = masterPassword ?? throw new ArgumentNullException(nameof(masterPassword));
    }

    /// <summary>
    /// Gets the unlocked vault snapshot.
    /// </summary>
    public SecretVault Vault { get; }

    /// <summary>
    /// Gets the master password used for persistence.
    /// </summary>
    public MasterPassword MasterPassword { get; }

    /// <summary>
    /// Creates a new session snapshot with updated vault contents.
    /// </summary>
    /// <param name="vault">Updated vault snapshot.</param>
    /// <returns>A new session instance.</returns>
    public SecretVaultSession WithVault(SecretVault vault)
        => new(vault ?? throw new ArgumentNullException(nameof(vault)), MasterPassword);

    /// <summary>
    /// Creates a new session snapshot with a new master password.
    /// </summary>
    /// <param name="masterPassword">Updated master password.</param>
    /// <returns>A new session instance.</returns>
    public SecretVaultSession WithMasterPassword(MasterPassword masterPassword)
        => new(Vault, masterPassword ?? throw new ArgumentNullException(nameof(masterPassword)));
}
