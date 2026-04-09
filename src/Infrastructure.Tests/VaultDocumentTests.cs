using FluentAssertions;

using System.Text.Json;

namespace Infrastructure.Tests;

public sealed class VaultDocumentTests
{
    [Fact(DisplayName = "VaultDocument should default legacy payloads to document version one")]
    [Trait("Category", "Unit")]
    public void VaultDocumentShouldDefaultLegacyPayloadsToDocumentVersionOne()
    {
        // Arrange
        const string legacyPayload = """
            {
              "CreatedUtc": "2026-04-03T12:00:00+00:00",
              "UpdatedUtc": "2026-04-03T12:30:00+00:00",
              "Entries": []
            }
            """;

        // Act
        var document = JsonSerializer.Deserialize<VaultDocument>(legacyPayload);

        // Assert
        document.Should().NotBeNull();
        document!.DocumentVersion.Should().Be(1);
        document.Notes.Should().BeEmpty();
    }
}
