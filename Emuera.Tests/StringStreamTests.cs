using MinorShift.Emuera.Sub;
using Xunit;

namespace Emuera.Tests;

/// <summary>Tests for the <see cref="StringStream"/> parser helper.</summary>
public class StringStreamTests
{
    [Fact]
    public void EmptyString_IsAtEOS()
    {
        var ss = new StringStream("");
        Assert.True(ss.EOS);
        Assert.Equal(StringStream.EndOfString, ss.Current);
    }

    [Fact]
    public void Initial_CurrentIsFirstChar()
    {
        var ss = new StringStream("abc");
        Assert.Equal('a', ss.Current);
        Assert.False(ss.EOS);
    }

    [Fact]
    public void ShiftNext_AdvancesPointer()
    {
        var ss = new StringStream("abc");
        ss.ShiftNext();
        Assert.Equal('b', ss.Current);
        ss.ShiftNext();
        Assert.Equal('c', ss.Current);
        ss.ShiftNext();
        Assert.True(ss.EOS);
    }

    [Fact]
    public void RowString_ReturnsOriginal()
    {
        const string src = "hello";
        var ss = new StringStream(src);
        Assert.Equal(src, ss.RowString);
    }

    [Fact]
    public void CurrentPosition_CanBeSet()
    {
        var ss = new StringStream("abcde");
        ss.CurrentPosition = 3;
        Assert.Equal('d', ss.Current);
    }

    [Fact]
    public void NullString_TreatedAsEmpty()
    {
        var ss = new StringStream(null!);
        Assert.True(ss.EOS);
    }
}
