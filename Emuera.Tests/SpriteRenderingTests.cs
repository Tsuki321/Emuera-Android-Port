using System.Drawing;
using MinorShift.Emuera.Content;
using SkiaSharp;
using Xunit;

namespace Emuera.Tests;

public class SpriteRenderingTests
{
    [Fact]
    public void ImageLoaderResolvesMixedCaseAndWindowsSeparators()
    {
        string directory = Path.Combine(Path.GetTempPath(), "emuera-image-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(directory, "Images"));
        try
        {
            using var bitmap = new SKBitmap(1, 1);
            bitmap.SetPixel(0, 0, SKColors.Blue);
            using (var output = File.Create(Path.Combine(directory, "Images", "TEST.PNG")))
                bitmap.Encode(output, SKEncodedImageFormat.Png, 100);
            using var decoded = ImgUtils.LoadImage(directory + "/images\\test.png");
            Assert.NotNull(decoded);
            Assert.Equal(SKColors.Blue, decoded.GetPixel(0, 0));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void CroppedSpriteDrawsAtDestinationAndPreservesCanvasTransform()
    {
        using var source = new GraphicsImage(91);
        source.GCreate(4, 2, false);
        source.GClear(Color.Red);
        source.GSetColor(Color.Blue, 2, 0);
        using var sprite = new SpriteG("test", source, new Rectangle(2, 0, 1, 1));
        using var bitmap = new SKBitmap(8, 8);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        sprite.GraphicsDraw(canvas, new Rectangle(3, 4, 2, 2));
        Assert.Equal(SKColors.Blue, bitmap.GetPixel(3, 4));
        Assert.Equal(0, bitmap.GetPixel(0, 0).Alpha);
        Assert.Equal(SKMatrix.Identity, canvas.TotalMatrix);
    }

    [Fact]
    public void AnimatedSpriteDrawsItsFirstFrame()
    {
        using var source = new GraphicsImage(92);
        source.GCreate(2, 2, false);
        source.GClear(Color.Lime);
        using var sprite = new SpriteAnime("animation", new Size(2, 2));
        sprite.AddFrame(source, new Rectangle(0, 0, 2, 2), Point.Empty, 100);
        using var bitmap = new SKBitmap(4, 4);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        sprite.GraphicsDraw(canvas, new Point(1, 1));
        Assert.Equal(SKColors.Lime, bitmap.GetPixel(1, 1));
    }
}
