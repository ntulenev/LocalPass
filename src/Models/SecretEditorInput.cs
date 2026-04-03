namespace Models;

/// <summary>
/// Represents validated user input used to create or update a secret.
/// </summary>
public sealed class SecretEditorInput
{
    /// <summary>
    /// Initializes validated secret editor input.
    /// </summary>
    /// <param name="source">Validated secret source.</param>
    /// <param name="login">Validated secret login.</param>
    /// <param name="password">Validated secret password.</param>
    /// <param name="notes">Optional validated notes.</param>
    public SecretEditorInput(
        SecretSource source,
        SecretLogin login,
        SecretPassword password,
        SecretNotes? notes = null)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Login = login ?? throw new ArgumentNullException(nameof(login));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        Notes = notes ?? new SecretNotes(null);
    }

    /// <summary>
    /// Gets the validated secret source.
    /// </summary>
    public SecretSource Source { get; }

    /// <summary>
    /// Gets the validated secret login.
    /// </summary>
    public SecretLogin Login { get; }

    /// <summary>
    /// Gets the validated secret password.
    /// </summary>
    public SecretPassword Password { get; }

    /// <summary>
    /// Gets the optional validated notes.
    /// </summary>
    public SecretNotes Notes { get; }

    /// <summary>
    /// Creates validated input from raw editor field values.
    /// </summary>
    /// <param name="source">Raw source value.</param>
    /// <param name="login">Raw login value.</param>
    /// <param name="password">Raw password value.</param>
    /// <param name="notes">Raw notes value.</param>
    /// <returns>Validated input model.</returns>
    public static SecretEditorInput Create(
        string? source,
        string? login,
        string? password,
        string? notes)
        => new(
            new SecretSource(source),
            new SecretLogin(login),
            new SecretPassword(password),
            new SecretNotes(notes));

    /// <summary>
    /// Creates a new immutable secret record from this validated input.
    /// </summary>
    /// <param name="timestampUtc">Creation timestamp in UTC.</param>
    /// <returns>A new secret record.</returns>
    public SecretRecord ToRecord(DateTimeOffset timestampUtc)
        => SecretRecord.Create(
            Source.Value,
            Login.Value,
            Password.Value,
            Notes.Value,
            timestampUtc);

    /// <summary>
    /// Applies this validated input to an existing immutable secret record.
    /// </summary>
    /// <param name="secret">Existing secret to update.</param>
    /// <param name="updatedUtc">Update timestamp in UTC.</param>
    /// <returns>The updated secret record.</returns>
    public SecretRecord ApplyTo(SecretRecord secret, DateTimeOffset updatedUtc)
    {
        ArgumentNullException.ThrowIfNull(secret);

        return secret.Update(
            Source.Value,
            Login.Value,
            Password.Value,
            Notes.Value,
            updatedUtc);
    }
}
