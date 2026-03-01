using MinorShift.Emuera.Platform;
using Xunit;

namespace Emuera.Tests;

/// <summary>Tests for the <see cref="EngineFont"/> value type.</summary>
public class EngineFontTests
{
    [Fact]
    public void StyleFlags_AreIndependent()
    {
        var regular   = new EngineFont("Arial", 16f, EngineFontStyle.Regular);
        var bold      = new EngineFont("Arial", 16f, EngineFontStyle.Bold);
        var italic    = new EngineFont("Arial", 16f, EngineFontStyle.Italic);
        var underline = new EngineFont("Arial", 16f, EngineFontStyle.Underline);
        var strike    = new EngineFont("Arial", 16f, EngineFontStyle.Strikeout);

        Assert.False(regular.IsBold);
        Assert.False(regular.IsItalic);
        Assert.False(regular.IsUnderline);
        Assert.False(regular.IsStrikeout);

        Assert.True(bold.IsBold);
        Assert.True(italic.IsItalic);
        Assert.True(underline.IsUnderline);
        Assert.True(strike.IsStrikeout);
    }

    [Fact]
    public void CombinedStyles_AreAllSet()
    {
        var font = new EngineFont("Arial", 12f, EngineFontStyle.Bold | EngineFontStyle.Italic | EngineFontStyle.Underline);
        Assert.True(font.IsBold);
        Assert.True(font.IsItalic);
        Assert.True(font.IsUnderline);
        Assert.False(font.IsStrikeout);
    }

    [Fact]
    public void Properties_RoundTrip()
    {
        var font = new EngineFont("Meiryo", 20f, EngineFontStyle.Regular);
        Assert.Equal("Meiryo", font.FamilyName);
        Assert.Equal(20f, font.SizeInPixels);
        Assert.Equal(EngineFontStyle.Regular, font.Style);
    }
}
