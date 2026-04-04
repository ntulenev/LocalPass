using FluentAssertions;

using Models;

namespace Infrastructure.Tests;

public sealed class LocalPassViewFormatterTests
{
    [Fact(DisplayName = "BuildListItems should create indexed labels for each vault entry")]
    [Trait("Category", "Unit")]
    public void BuildListItemsShouldCreateIndexedLabelsForEachVaultEntry()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var first = SecretRecord.Create("Alpha", "first", "Password123!", null, timestamp);
        var second = SecretRecord.Create("Beta", "second", "Password123!", null, timestamp);
        var vault = new SecretVault([first, second], timestamp, timestamp);

        // Act
        var items = LocalPassViewFormatter.BuildListItems(vault);

        // Assert
        items.Should().ContainInOrder(
            "[1] Alpha | first",
            "[2] Beta | second");
    }

    [Fact(DisplayName = "BuildSummary should include the selected secret when available")]
    [Trait("Category", "Unit")]
    public void BuildSummaryShouldIncludeTheSelectedSecretWhenAvailable()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var secret = SecretRecord.Create("GitHub", "user@example.com", "Password123!", null, timestamp);
        var vault = new SecretVault([secret], timestamp, timestamp);

        // Act
        var summary = LocalPassViewFormatter.BuildSummary(vault, secret);

        // Assert
        summary.Should().Be("VAULT 001  LAST WRITE 2026-04-03 12:00 UTC  DOC V001  TARGET GitHub / user@example.com");
    }

    [Fact(DisplayName = "BuildSummary should show none when nothing is selected")]
    [Trait("Category", "Unit")]
    public void BuildSummaryShouldShowNoneWhenNothingIsSelected()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var vault = SecretVault.CreateEmpty(timestamp);

        // Act
        var summary = LocalPassViewFormatter.BuildSummary(vault, null);

        // Assert
        summary.Should().Be("VAULT 000  LAST WRITE 2026-04-03 12:00 UTC  DOC V001  TARGET none");
    }

    [Fact(DisplayName = "BuildDetails should instruct the user to create a record when the vault is empty")]
    [Trait("Category", "Unit")]
    public void BuildDetailsShouldInstructTheUserToCreateARecordWhenTheVaultIsEmpty()
    {
        // Act
        var details = LocalPassViewFormatter.BuildDetails(null, revealPasswords: false);

        // Assert
        details.Should().Be("No secrets indexed.\n\nPress N to create the first record.");
    }

    [Fact(DisplayName = "BuildDetails should mask the password when reveal is disabled")]
    [Trait("Category", "Unit")]
    public void BuildDetailsShouldMaskThePasswordWhenRevealIsDisabled()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var secret = SecretRecord.Create("GitHub", "user@example.com", "Password123!", null, timestamp);

        // Act
        var details = LocalPassViewFormatter.BuildDetails(secret, revealPasswords: false);

        // Assert
        details.Should().Contain("PASSWORD  ************ (12 chars hidden)");
        details.Should().Contain("NOTES     (none)");
    }

    [Fact(DisplayName = "BuildDetails should reveal the password when explicitly requested")]
    [Trait("Category", "Unit")]
    public void BuildDetailsShouldRevealThePasswordWhenExplicitlyRequested()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var secret = SecretRecord.Create("GitHub", "user@example.com", "Password123!", "primary", timestamp);

        // Act
        var details = LocalPassViewFormatter.BuildDetails(secret, revealPasswords: true);

        // Assert
        details.Should().Contain("PASSWORD  Password123!");
        details.Should().Contain("NOTES     primary");
        details.Should().Contain($"RECORD ID {secret.Id}");
    }

    [Fact(DisplayName = "FormatStatus should prepend the ready marker")]
    [Trait("Category", "Unit")]
    public void FormatStatusShouldPrependTheReadyMarker()
    {
        // Act
        var status = LocalPassViewFormatter.FormatStatus("Vault synced.");

        // Assert
        status.Should().Be("[ READY ] Vault synced.");
    }

    [Theory(DisplayName = "BuildMaskedPassword should clamp the amount of visible mask characters")]
    [InlineData("", "******** (0 chars hidden)")]
    [InlineData("short", "******** (5 chars hidden)")]
    [InlineData("123456789", "********* (9 chars hidden)")]
    [InlineData("12345678901234567890", "**************** (20 chars hidden)")]
    [Trait("Category", "Unit")]
    public void BuildMaskedPasswordShouldClampTheAmountOfVisibleMaskCharacters(
        string password,
        string expected)
    {
        // Act
        var masked = LocalPassViewFormatter.BuildMaskedPassword(password);

        // Assert
        masked.Should().Be(expected);
    }
}
