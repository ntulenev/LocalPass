using FluentAssertions;

using Terminal.Gui;

namespace Infrastructure.Tests;

public sealed class LocalPassConsoleLayoutTests
{
    [Fact(DisplayName = "CreateWindow should configure the main console shell")]
    [Trait("Category", "Unit")]
    public void CreateWindowShouldConfigureTheMainConsoleShell()
    {
        var chromeScheme = LocalPassConsoleTheme.CreateChromeScheme();

        var window = LocalPassConsoleLayout.CreateWindow(chromeScheme);

        window.Title.ToString().Should().Be("LocalPass :: Vault Console");
        window.X.ToString().Should().Be(Pos.At(0).ToString());
        window.Y.ToString().Should().Be(Pos.At(0).ToString());
        window.Width.ToString().Should().Be(Dim.Fill().ToString());
        window.Height.ToString().Should().Be(Dim.Fill(1).ToString());
        window.ColorScheme.Should().BeSameAs(chromeScheme);
    }

    [Fact(DisplayName = "CreateSecretListView should configure the list view for keyboard focus")]
    [Trait("Category", "Unit")]
    public void CreateSecretListViewShouldConfigureTheListViewForKeyboardFocus()
    {
        var focusScheme = LocalPassConsoleTheme.CreateFocusScheme();

        var listView = LocalPassConsoleLayout.CreateSecretListView(focusScheme);

        listView.X.ToString().Should().Be(Pos.At(0).ToString());
        listView.Y.ToString().Should().Be(Pos.At(0).ToString());
        listView.Width.ToString().Should().Be(Dim.Fill().ToString());
        listView.Height.ToString().Should().Be(Dim.Fill().ToString());
        listView.CanFocus.Should().BeTrue();
        listView.ColorScheme.Should().BeSameAs(focusScheme);
    }

    [Fact(DisplayName = "CreatePayloadInspectFrame should anchor to the secret index frame")]
    [Trait("Category", "Unit")]
    public void CreatePayloadInspectFrameShouldAnchorToTheSecretIndexFrame()
    {
        var chromeScheme = LocalPassConsoleTheme.CreateChromeScheme();
        var secretIndexFrame = LocalPassConsoleLayout.CreateSecretIndexFrame(chromeScheme);

        var payloadInspectFrame = LocalPassConsoleLayout.CreatePayloadInspectFrame(
            chromeScheme,
            secretIndexFrame);

        payloadInspectFrame.X.ToString().Should().Be(Pos.Right(secretIndexFrame).ToString());
        payloadInspectFrame.Y.ToString().Should().Be(Pos.At(3).ToString());
        payloadInspectFrame.Width.ToString().Should().Be(Dim.Fill().ToString());
        payloadInspectFrame.Height.ToString().Should().Be(Dim.Fill().ToString());
        payloadInspectFrame.Title.ToString().Should().Be("Payload Inspect");
        payloadInspectFrame.ColorScheme.Should().BeSameAs(chromeScheme);
    }

    [Fact(DisplayName = "CreatePayloadDetailsView should configure the read-only details pane")]
    [Trait("Category", "Unit")]
    public void CreatePayloadDetailsViewShouldConfigureTheReadOnlyDetailsPane()
    {
        var accentScheme = LocalPassConsoleTheme.CreateAccentScheme();

        var detailsView = LocalPassConsoleLayout.CreatePayloadDetailsView(accentScheme);

        detailsView.X.ToString().Should().Be(Pos.At(0).ToString());
        detailsView.Y.ToString().Should().Be(Pos.At(0).ToString());
        detailsView.Width.ToString().Should().Be(Dim.Fill().ToString());
        detailsView.Height.ToString().Should().Be(Dim.Fill().ToString());
        detailsView.ReadOnly.Should().BeTrue();
        detailsView.WordWrap.Should().BeTrue();
        detailsView.CanFocus.Should().BeFalse();
        detailsView.ColorScheme.Should().BeSameAs(accentScheme);
    }
}
