using System.Text;
using MinorShift.Emuera.Sub;
using Xunit;

namespace Emuera.Tests;

public class EncodingHandlerTests
{
    [Fact]
    public void ShiftJisAfterLongAsciiHeaderIsDetected()
    {
        byte[] data = EncodingHandler.shiftjisEncoding.GetBytes(new string(';', 20000) + "\n日本語");
        using var stream = new MemoryStream(data);
        Assert.Equal(932, EncodingHandler.DetectEncoding(stream).CodePage);
        Assert.Equal(0, stream.Position);
        Assert.True(stream.CanRead);
    }

    [Fact]
    public void Utf8IsDetectedAndStreamPositionIsPreserved()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("skip日本語中文한국어"));
        stream.Position = 4;
        Assert.Equal(65001, EncodingHandler.DetectEncoding(stream).CodePage);
        Assert.Equal(4, stream.Position);
    }

    [Fact]
    public void Utf16BomTakesPrecedence()
    {
        using var stream = new MemoryStream(Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("日本語")).ToArray());
        Assert.Equal(1200, EncodingHandler.DetectEncoding(stream).CodePage);
        Assert.Equal(0, stream.Position);
    }
}
