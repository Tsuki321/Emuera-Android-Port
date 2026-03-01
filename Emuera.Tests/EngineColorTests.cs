using MinorShift.Emuera.Platform;
using Xunit;

namespace Emuera.Tests;

/// <summary>Tests for the <see cref="EngineColor"/> value type.</summary>
public class EngineColorTests
{
    [Fact]
    public void DefaultAlphaIs255()
    {
        var color = new EngineColor(10, 20, 30);
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void FromHtml_ParsesSixDigit()
    {
        var color = EngineColor.FromHtml("#FF8000");
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(0,   color.B);
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void FromHtml_ParsesEightDigit()
    {
        var color = EngineColor.FromHtml("#80FF8000");
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(0,   color.B);
        Assert.Equal(128, color.A);
    }

    [Fact]
    public void ToString_ProducesHexString()
    {
        var color = new EngineColor(255, 128, 0);
        Assert.Equal("#FF8000", color.ToString());
    }

    [Fact]
    public void ToArgb_ReturnsCorrectValue()
    {
        var color = new EngineColor(1, 2, 3, 4);
        // A=4, R=1, G=2, B=3  → 0x04010203
        Assert.Equal((4 << 24) | (1 << 16) | (2 << 8) | 3, color.ToArgb());
    }

    [Fact]
    public void WellKnownConstants_AreCorrect()
    {
        Assert.Equal(new EngineColor(0, 0, 0), EngineColor.Black);
        Assert.Equal(new EngineColor(255, 255, 255), EngineColor.White);
    }

    [Fact]
    public void FromArgb_ThreeParam_SetsRgb()
    {
        var color = EngineColor.FromArgb(10, 20, 30);
        Assert.Equal(10, color.R);
        Assert.Equal(20, color.G);
        Assert.Equal(30, color.B);
        Assert.Equal(255, color.A);
    }
}
