using FluentAssertions;

namespace Infrastructure.Tests;

public sealed class VaultEnvelopeSerializerTests
{
    [Fact(DisplayName = "Serialize and deserialize should round-trip the encrypted envelope")]
    [Trait("Category", "Unit")]
    public void SerializeAndDeserializeShouldRoundTripTheEncryptedEnvelope()
    {
        // Arrange
        var document = new EncryptedVaultFileDocument
        {
            Version = 1,
            Kdf = "PBKDF2-SHA512",
            Encryption = "AES-256-GCM",
            Iterations = 600000,
            Salt = "salt",
            Nonce = "nonce",
            Tag = "tag",
            CipherText = "cipher"
        };

        // Act
        var json = VaultEnvelopeSerializer.Serialize(document);
        var roundTrippedDocument = VaultEnvelopeSerializer.Deserialize(json);

        // Assert
        roundTrippedDocument.Version.Should().Be(document.Version);
        roundTrippedDocument.Kdf.Should().Be(document.Kdf);
        roundTrippedDocument.Encryption.Should().Be(document.Encryption);
        roundTrippedDocument.Iterations.Should().Be(document.Iterations);
        roundTrippedDocument.Salt.Should().Be(document.Salt);
        roundTrippedDocument.Nonce.Should().Be(document.Nonce);
        roundTrippedDocument.Tag.Should().Be(document.Tag);
        roundTrippedDocument.CipherText.Should().Be(document.CipherText);
    }

    [Fact(DisplayName = "Deserialize should throw when the envelope JSON is invalid")]
    [Trait("Category", "Unit")]
    public void DeserializeShouldThrowWhenTheEnvelopeJsonIsInvalid()
    {
        // Arrange
        // Act
        var action = () => VaultEnvelopeSerializer.Deserialize("{ invalid json");

        // Assert
        var exception = action.Should().Throw<InvalidDataException>().Which;
        exception.Message.Should().Be("Vault file is invalid.");
    }
}
