using Abstractions;

using FluentAssertions;

using Models;

namespace Infrastructure.Tests;

public sealed class LocalPassViewFormatterTests
{
    [Fact(DisplayName = "BuildPasswordListItems should create indexed labels for each vault entry")]
    [Trait("Category", "Unit")]
    public void BuildPasswordListItemsShouldCreateIndexedLabelsForEachVaultEntry()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var first = SecretRecord.Create("Alpha", "first", "Password123!", null, timestamp);
        var second = SecretRecord.Create("Beta", "second", "Password123!", null, timestamp);

        var items = LocalPassViewFormatter.BuildPasswordListItems([first, second]);

        items.Should().ContainInOrder(
            "[1] Alpha | first",
            "[2] Beta | second");
    }

    [Fact(DisplayName = "BuildNoteListItems should create indexed labels for each secure note")]
    [Trait("Category", "Unit")]
    public void BuildNoteListItemsShouldCreateIndexedLabelsForEachSecureNote()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var first = SecureNoteRecord.Create("Server", "prod access", "keep this safe", timestamp);
        var second = SecureNoteRecord.Create("Tax", "2025 docs", "important", timestamp);

        var items = LocalPassViewFormatter.BuildNoteListItems([first, second]);

        items.Should().ContainInOrder(
            "[1] Server | prod access",
            "[2] Tax | 2025 docs");
    }

    [Fact(DisplayName = "BuildSummary should include counts, active tab, and selected label")]
    [Trait("Category", "Unit")]
    public void BuildSummaryShouldIncludeCountsActiveTabAndSelectedLabel()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var secret = SecretRecord.Create("GitHub", "user@example.com", "Password123!", null, timestamp);
        var note = SecureNoteRecord.Create("Taxes", "2025 filing", "very important", timestamp);
        var vault = new SecretVault([secret], [note], timestamp, timestamp);

        var summary = LocalPassViewFormatter.BuildSummary(
            vault,
            LocalPassVaultTab.Passwords,
            "GitHub / user@example.com");

        summary.Should().Be(
            "PASSWORDS 001  NOTES 001  ACTIVE PASSWORDS  LAST WRITE 2026-04-03 12:00 UTC  DOC V001  TARGET GitHub / user@example.com");
    }

    [Fact(DisplayName = "BuildSummary should show none when nothing is selected")]
    [Trait("Category", "Unit")]
    public void BuildSummaryShouldShowNoneWhenNothingIsSelected()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var vault = SecretVault.CreateEmpty(timestamp);

        var summary = LocalPassViewFormatter.BuildSummary(vault, LocalPassVaultTab.Notes, null);

        summary.Should().Be(
            "PASSWORDS 000  NOTES 000  ACTIVE NOTES  LAST WRITE 2026-04-03 12:00 UTC  DOC V001  TARGET none");
    }

    [Fact(DisplayName = "BuildPasswordDetails should instruct the user to create a record when the tab is empty")]
    [Trait("Category", "Unit")]
    public void BuildPasswordDetailsShouldInstructTheUserToCreateARecordWhenTheTabIsEmpty()
    {
        var details = LocalPassViewFormatter.BuildPasswordDetails(null, revealPasswords: false);

        details.Should().Be("No passwords indexed.\n\nPress N to create the first record.");
    }

    [Fact(DisplayName = "BuildPasswordDetails should mask the password when reveal is disabled")]
    [Trait("Category", "Unit")]
    public void BuildPasswordDetailsShouldMaskThePasswordWhenRevealIsDisabled()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var secret = SecretRecord.Create("GitHub", "user@example.com", "Password123!", null, timestamp);

        var details = LocalPassViewFormatter.BuildPasswordDetails(secret, revealPasswords: false);

        details.Should().Contain("PASSWORD  ************ (12 chars hidden)");
        details.Should().Contain("NOTES     (none)");
    }

    [Fact(DisplayName = "BuildPasswordDetails should reveal the password when explicitly requested")]
    [Trait("Category", "Unit")]
    public void BuildPasswordDetailsShouldRevealThePasswordWhenExplicitlyRequested()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var secret = SecretRecord.Create("GitHub", "user@example.com", "Password123!", "primary", timestamp);

        var details = LocalPassViewFormatter.BuildPasswordDetails(secret, revealPasswords: true);

        details.Should().Contain("PASSWORD  Password123!");
        details.Should().Contain("NOTES     primary");
        details.Should().Contain($"RECORD ID {secret.Id}");
    }

    [Fact(DisplayName = "BuildNoteDetails should render note content and metadata")]
    [Trait("Category", "Unit")]
    public void BuildNoteDetailsShouldRenderNoteContentAndMetadata()
    {
        var timestamp = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        var note = SecureNoteRecord.Create("Taxes", "2025 filing", "line 1\nline 2", timestamp);

        var details = LocalPassViewFormatter.BuildNoteDetails(note);

        details.Should().Contain("TITLE      Taxes");
        details.Should().Contain("SUMMARY    2025 filing");
        details.Should().Contain("line 1\nline 2");
        details.Should().Contain($"NOTE ID    {note.Id}");
    }

    [Fact(DisplayName = "BuildIndexTitle should highlight the active tab")]
    [Trait("Category", "Unit")]
    public void BuildIndexTitleShouldHighlightTheActiveTab()
    {
        LocalPassViewFormatter.BuildIndexTitle(LocalPassVaultTab.Passwords)
            .Should()
            .Be("Vault Index :: [Passwords] | Notes  (Tab switch)");

        LocalPassViewFormatter.BuildIndexTitle(LocalPassVaultTab.Notes)
            .Should()
            .Be("Vault Index :: Passwords | [Notes]  (Tab switch)");
    }

    [Fact(DisplayName = "FormatStatus should prepend the ready marker")]
    [Trait("Category", "Unit")]
    public void FormatStatusShouldPrependTheReadyMarker()
    {
        var status = LocalPassViewFormatter.FormatStatus("Vault synced.");

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
        var masked = LocalPassViewFormatter.BuildMaskedPassword(password);

        masked.Should().Be(expected);
    }

}
