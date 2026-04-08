using System.Security.Cryptography;

namespace Infrastructure;

/// <summary>
/// Generates cryptographically secure strong passwords for stored secrets.
/// </summary>
public static class StrongPasswordGenerator
{
    /// <summary>
    /// Generates a strong password that includes uppercase, lowercase, digit, and symbol characters.
    /// </summary>
    /// <param name="length">Requested password length.</param>
    /// <returns>The generated password.</returns>
    public static string Generate(int length = DefaultLength)
    {
        if (length < RequiredCharacterSets)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Password length must be at least 4 characters.");
        }

        var buffer = new char[length];
        buffer[0] = GetRandomCharacter(UppercaseCharacters);
        buffer[1] = GetRandomCharacter(LowercaseCharacters);
        buffer[2] = GetRandomCharacter(DigitCharacters);
        buffer[3] = GetRandomCharacter(SymbolCharacters);

        for (var index = RequiredCharacterSets; index < buffer.Length; index++)
        {
            buffer[index] = GetRandomCharacter(AllCharacters);
        }

        Shuffle(buffer);
        return new string(buffer);
    }

    private static char GetRandomCharacter(string characters)
        => characters[RandomNumberGenerator.GetInt32(characters.Length)];

    private static void Shuffle(Span<char> buffer)
    {
        for (var index = buffer.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (buffer[index], buffer[swapIndex]) = (buffer[swapIndex], buffer[index]);
        }
    }

    private const int DefaultLength = 20;
    private const int RequiredCharacterSets = 4;
    private const string UppercaseCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowercaseCharacters = "abcdefghijkmnopqrstuvwxyz";
    private const string DigitCharacters = "23456789";
    private const string SymbolCharacters = "!@#$%^&*()-_=+[]{}.,?";
    private const string AllCharacters =
        UppercaseCharacters + LowercaseCharacters + DigitCharacters + SymbolCharacters;
}
