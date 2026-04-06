using FluentAssertions;

using Terminal.Gui;

namespace Infrastructure.Tests;

public sealed class LocalPassConsoleThemeTests
{
    [Fact(DisplayName = "CreateAccentScheme should configure the expected palette")]
    [Trait("Category", "Unit")]
    public void CreateAccentSchemeShouldConfigureTheExpectedPalette()
    {
        var scheme = LocalPassConsoleTheme.CreateAccentScheme();

        scheme.Normal.Should().Be(Terminal.Gui.Attribute.Make(Color.Green, Color.Black));
        scheme.Focus.Should().Be(Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black));
        scheme.HotNormal.Should().Be(Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black));
        scheme.HotFocus.Should().Be(Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black));
        scheme.Disabled.Should().Be(Terminal.Gui.Attribute.Make(Color.DarkGray, Color.Black));
    }

    [Fact(DisplayName = "CreateChromeScheme should configure the expected palette")]
    [Trait("Category", "Unit")]
    public void CreateChromeSchemeShouldConfigureTheExpectedPalette()
    {
        var scheme = LocalPassConsoleTheme.CreateChromeScheme();

        scheme.Normal.Should().Be(Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black));
        scheme.Focus.Should().Be(Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black));
        scheme.HotNormal.Should().Be(Terminal.Gui.Attribute.Make(Color.Green, Color.Black));
        scheme.HotFocus.Should().Be(Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black));
        scheme.Disabled.Should().Be(Terminal.Gui.Attribute.Make(Color.DarkGray, Color.Black));
    }

    [Fact(DisplayName = "CreateFocusScheme should configure the expected palette")]
    [Trait("Category", "Unit")]
    public void CreateFocusSchemeShouldConfigureTheExpectedPalette()
    {
        var scheme = LocalPassConsoleTheme.CreateFocusScheme();

        scheme.Normal.Should().Be(Terminal.Gui.Attribute.Make(Color.Green, Color.Black));
        scheme.Focus.Should().Be(Terminal.Gui.Attribute.Make(Color.Black, Color.BrightGreen));
        scheme.HotNormal.Should().Be(Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black));
        scheme.HotFocus.Should().Be(Terminal.Gui.Attribute.Make(Color.Black, Color.BrightGreen));
        scheme.Disabled.Should().Be(Terminal.Gui.Attribute.Make(Color.DarkGray, Color.Black));
    }
}
