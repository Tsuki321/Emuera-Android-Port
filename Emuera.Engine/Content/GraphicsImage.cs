using MinorShift._Library;
using MinorShift.Emuera.Platform;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using SkiaSharp;
using FontStyle = MinorShift.Emuera.Platform.EngineFontStyle;

namespace MinorShift.Emuera.Content;

internal sealed class GraphicsImage : AbstractImage
{
	public GraphicsImage(int id)
	{
		ID = id;
		g = null;
		RealBitmap = null;
	}
	public readonly int ID;
	Size size;
	SKPaint brushPaint = null;
	SKPaint penPaint = null;
	EngineFont? efont = null;
	EngineFontStyle efontStyle = default;

	public bool useImgList { get { return drawImgList != null; } }
	public List<Tuple<ASprite, Rectangle>> drawImgList = null;

	SKCanvas g;

	public SKBitmap RealBitmap;
	public override SKBitmap Bitmap
	{
		set { RealBitmap = value; }
		get
		{
			Load();
			return RealBitmap;
		}
	}

	#region Bitmap書き込み・作成

	public void GCreate(int x, int y, bool useGDI)
	{
		this.GDispose();
		RealBitmap = new SKBitmap(x, y, SKColorType.Bgra8888, SKAlphaType.Premul);
		g = new SKCanvas(RealBitmap);
		size = new Size(x, y);
		drawImgList = new List<Tuple<ASprite, Rectangle>>();
		AppContents.tempLoadedGraphicsImages.Add(this);
	}

	internal void GCreateFromF(SKBitmap bmp, bool useGDI)
	{
		this.GDispose();
		RealBitmap = bmp.Copy(SKColorType.Bgra8888);
		g = new SKCanvas(RealBitmap);
		size = new Size(RealBitmap.Width, RealBitmap.Height);
	}

	public void GClear(Color c)
	{
		if (g == null)
			throw new NullReferenceException();
		g.Clear(new SKColor(c.R, c.G, c.B, c.A));
	}

	public void GClear(Color c, int x, int y, int w, int h)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();
		g.Save();
		g.ClipRect(new SKRect(x, y, x + w, y + h));
		g.Clear(new SKColor(c.R, c.G, c.B, c.A));
		g.Restore();
		drawImgList = null;
	}

	/// <summary>Creates an SKPaint configured for text drawing using the currently-set font.</summary>
	private SKPaint CreateTextPaint(out SKTypeface typeface)
	{
		EngineFont font = efont ?? Config.Font;
		typeface = SKTypeface.FromFamilyName(
			font.FamilyName,
			new SKFontStyle(
				font.IsBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
				SKFontStyleWidth.Normal,
				font.IsItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright));
		return new SKPaint
		{
			Typeface = typeface,
			TextSize = font.SizeInPixels,
			Color = brushPaint?.Color ?? new SKColor(Config.ForeColor.R, Config.ForeColor.G, Config.ForeColor.B, Config.ForeColor.A),
			IsAntialias = true,
		};
	}

	public void GDrawString(string text, int x, int y)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();
		EngineFont font = efont ?? Config.Font;
		using var paint = CreateTextPaint(out var typeface);
		using (typeface)
		// SkiaSharp DrawText y is the baseline; shift down by TextSize to top-align
		g.DrawText(text, x, y + font.SizeInPixels, paint);
	}

	public void GDrawString(string text, int x, int y, int width, int height)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();
		EngineFont font = efont ?? Config.Font;
		using var paint = CreateTextPaint(out var typeface);
		using (typeface)
		{
			g.Save();
			g.ClipRect(new SKRect(x, y, x + width, y + height));
			g.DrawText(text, x, y + font.SizeInPixels, paint);
			g.Restore();
		}
	}

	public void GDrawRectangle(Rectangle rect)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();
		drawImgList = null;
		using var paint = penPaint != null
			? penPaint.Clone()
			: new SKPaint { Color = new SKColor(Config.ForeColor.R, Config.ForeColor.G, Config.ForeColor.B, Config.ForeColor.A), IsStroke = true };
		g.DrawRect(new SKRect(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height), paint);
	}

	public void GFillRectangle(Rectangle rect)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();
		drawImgList = null;
		using var paint = brushPaint != null
			? brushPaint.Clone()
			: new SKPaint { Color = new SKColor(Config.BackColor.R, Config.BackColor.G, Config.BackColor.B, Config.BackColor.A) };
		g.DrawRect(new SKRect(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height), paint);
	}

	public void GDrawCImg(ASprite img, Rectangle destRect)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();
		if (useImgList)
		{
			if (img is SpriteG imgG)
			{
				if (imgG.useImgList)
				{
					foreach (Tuple<ASprite, Rectangle> img_element in imgG.drawImgList)
					{
						if (imgG.isBaseImage(this))
						{
							drawImgList = null;
							break;
						}
						drawImgList.Add(new Tuple<ASprite, Rectangle>(
							img_element.Item1,
							new Rectangle(
								(img_element.Item2.X + destRect.X) * destRect.Width / imgG.DestBaseSize.Width,
								(img_element.Item2.Y + destRect.Y) * destRect.Height / imgG.DestBaseSize.Height,
								img_element.Item2.Width * destRect.Width / imgG.DestBaseSize.Width,
								img_element.Item2.Height * destRect.Height / imgG.DestBaseSize.Height
							)
						));
					}
				}
				else
				{
					drawImgList = null;
				}
			}
			else if (img is SpriteF)
			{
				drawImgList.Add(new Tuple<ASprite, Rectangle>(img, destRect));
				if (drawImgList.Count > 50)
					drawImgList = null;
			}
			else
			{
				drawImgList = null;
			}
		}
		img.GraphicsDraw(g, destRect);
	}

	public void GDrawCImg(ASprite img, Rectangle destRect, float[][] cm)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();
		drawImgList = null;
		// Phase 3: implement color-matrix support via SKColorFilter
		img.GraphicsDraw(g, destRect, cm);
	}

	public void GDrawG(GraphicsImage srcGra, Rectangle destRect, Rectangle srcRect)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();
		drawImgList = null;
		SKBitmap src = srcGra.GetSKBitmap();
		using var paint = new SKPaint();
		var srcSkRect = new SKRect(srcRect.X, srcRect.Y, srcRect.X + srcRect.Width, srcRect.Y + srcRect.Height);
		var destSkRect = new SKRect(destRect.X, destRect.Y, destRect.X + destRect.Width, destRect.Y + destRect.Height);
		g.DrawBitmap(src, srcSkRect, destSkRect, paint);
	}

	public void GDrawG(GraphicsImage srcGra, Rectangle destRect, Rectangle srcRect, float[][] cm)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();
		drawImgList = null;
		// Phase 3: color matrix support
		SKBitmap src = srcGra.GetSKBitmap();
		using var paint = new SKPaint();
		var srcSkRect = new SKRect(srcRect.X, srcRect.Y, srcRect.X + srcRect.Width, srcRect.Y + srcRect.Height);
		var destSkRect = new SKRect(destRect.X, destRect.Y, destRect.X + destRect.Width, destRect.Y + destRect.Height);
		g.DrawBitmap(src, srcSkRect, destSkRect, paint);
	}

	public void GDrawGWithMask(GraphicsImage srcGra, GraphicsImage maskGra, Point destPoint)
	{
		Load();
		if (g == null)
			throw new NullReferenceException();
		drawImgList = null;

		// Apply the mask image as an alpha mask: for each pixel in src, multiply its
		// alpha by the red channel of the corresponding mask pixel, then composite
		// the result onto this (destination) bitmap at destPoint.
		SKBitmap src  = srcGra.GetSKBitmap();
		SKBitmap mask = maskGra.GetSKBitmap();

		int w = src.Width;
		int h = src.Height;
		// Guard: mask must cover the entire source area
		if (mask.Width < w || mask.Height < h)
			return;

		using var masked = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
		SKColor[] srcPixels  = src.Pixels;
		SKColor[] maskPixels = mask.Pixels;
		SKColor[] dstPixels  = new SKColor[w * h];

		int maskW = mask.Width;
		for (int row = 0; row < h; row++)
		{
			for (int col = 0; col < w; col++)
			{
				int srcIdx  = row * w + col;
				int maskIdx = row * maskW + col;
				SKColor s = srcPixels[srcIdx];
				// Use red channel of the mask as the alpha weight
				float maskAlpha = maskPixels[maskIdx].Red / 255f;
				dstPixels[srcIdx] = new SKColor(s.Red, s.Green, s.Blue, (byte)(s.Alpha * maskAlpha));
			}
		}
		masked.Pixels = dstPixels;

		using var paint = new SKPaint();
		g.DrawBitmap(masked, destPoint.X, destPoint.Y, paint);
	}

	public void GRotate(Int64 a, int x, int y)
	{
		if (g == null)
			throw new NullReferenceException();
		// a is the rotation angle in degrees (0–360)
		g.RotateDegrees((float)a, x, y);
	}

	public void GDrawGWithRotate(GraphicsImage srcGra, Int64 a, int x, int y)
	{
		if (g == null || srcGra == null)
			throw new NullReferenceException();
		drawImgList = null;
		SKBitmap src = srcGra.GetSKBitmap();
		g.Save();
		// Rotate around the given pivot point, then draw at (0,0) so the bitmap
		// is composited into the destination with the rotation applied.
		g.RotateDegrees((float)a, x, y);
		using var paint = new SKPaint();
		g.DrawBitmap(src, 0, 0, paint);
		g.Restore();
	}

	public void GDrawLine(int fromX, int fromY, int forX, int forY)
	{
		if (g == null)
			throw new NullReferenceException();
		using var paint = penPaint != null
			? penPaint.Clone()
			: new SKPaint { Color = new SKColor(Config.ForeColor.R, Config.ForeColor.G, Config.ForeColor.B, Config.ForeColor.A), IsStroke = true };
		g.DrawLine(fromX, fromY, forX, forY, paint);
	}

	public void GDashStyle(long style, long cap)
	{
		// Phase 3: map to SKPathEffect for dashes
		if (g == null)
			throw new NullReferenceException();
		if (penPaint == null)
			penPaint = new SKPaint { Color = new SKColor(Config.ForeColor.R, Config.ForeColor.G, Config.ForeColor.B, Config.ForeColor.A), IsStroke = true };
		// Dash mapping is Phase 3; silently ignore for now.
	}

	public void GSetFont(EngineFont r, EngineFontStyle fs)
	{
		efont = r;
		efontStyle = fs;
	}

	public void GSetBrush(Color c)
	{
		brushPaint?.Dispose();
		brushPaint = new SKPaint { Color = new SKColor(c.R, c.G, c.B, c.A) };
	}

	public void GSetPen(Color c, float width)
	{
		penPaint?.Dispose();
		penPaint = new SKPaint { Color = new SKColor(c.R, c.G, c.B, c.A), IsStroke = true, StrokeWidth = width };
	}

	public SKBitmap GetSKBitmap()
	{
		if (Bitmap == null)
			throw new NullReferenceException();
		return Bitmap;
	}

	public void GSetColor(Color c, int x, int y)
	{
		if (RealBitmap == null)
			throw new NullReferenceException();
		RealBitmap.SetPixel(x, y, new SKColor(c.R, c.G, c.B, c.A));
	}

	public Color GGetColor(int x, int y)
	{
		if (RealBitmap == null)
			throw new NullReferenceException();
		SKColor sk = RealBitmap.GetPixel(x, y);
		return Color.FromArgb(sk.Alpha, sk.Red, sk.Green, sk.Blue);
	}

	public void UnLoad()
	{
		if (RealBitmap == null)
			return;
		g?.Dispose();
		RealBitmap.Dispose();
		g = null;
		RealBitmap = null;
	}

	public void GDispose()
	{
		size = new Size(0, 0);
		drawImgList = null;
		g?.Dispose();
		RealBitmap?.Dispose();
		brushPaint?.Dispose();
		penPaint?.Dispose();
		g = null;
		RealBitmap = null;
		brushPaint = null;
		penPaint = null;
		efont = null;
	}

	public override void Dispose()
	{
		this.GDispose();
	}

	~GraphicsImage()
	{
		Dispose();
	}

	public override bool IsCreated { get { return g != null || useImgList; } }
	public int Width { get { return size.Width; } }
	public int Height { get { return size.Height; } }

	public string Fontname { get { return efont?.FamilyName ?? ""; } }
	public int Fontsize { get { return efont.HasValue ? (int)efont.Value.SizeInPixels : 0; } }
	public EngineFont? Fnt { get { return efont; } }

	public int Fontstyle
	{
		get
		{
			int ret = 0;
			if ((efontStyle & EngineFontStyle.Bold) != 0) ret |= 1;
			if ((efontStyle & EngineFontStyle.Italic) != 0) ret |= 2;
			if ((efontStyle & EngineFontStyle.Strikeout) != 0) ret |= 4;
			if ((efontStyle & EngineFontStyle.Underline) != 0) ret |= 8;
			return ret;
		}
	}

	/// <summary>Returns the pen color as ARGB, or 0 if no pen set.</summary>
	public Color PenColor { get { return penPaint != null ? Color.FromArgb(penPaint.Color.Alpha, penPaint.Color.Red, penPaint.Color.Green, penPaint.Color.Blue) : Color.Black; } }
	/// <summary>Returns the pen stroke width, or 1f if no pen set.</summary>
	public float PenWidth { get { return penPaint?.StrokeWidth ?? 1f; } }
	/// <summary>Returns the brush color as ARGB, or white if no brush set.</summary>
	public Color BrushColor { get { return brushPaint != null ? Color.FromArgb(brushPaint.Color.Alpha, brushPaint.Color.Red, brushPaint.Color.Green, brushPaint.Color.Blue) : Color.White; } }

	public bool GBitmapToInt64Array(Int64[,] array, int xstart, int ystart)
	{
		if (g == null || RealBitmap == null)
			throw new NullReferenceException();
		int w = RealBitmap.Width;
		int h = RealBitmap.Height;
		if (xstart + w > array.GetLength(0) || ystart + h > array.GetLength(1))
			return false;
		SKColor[] pixels = RealBitmap.Pixels;
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				SKColor c = pixels[y * w + x];
				array[x + xstart, y + ystart] =
					c.Blue +
					(((Int64)c.Green) << 8) +
					(((Int64)c.Red) << 16) +
					(((Int64)c.Alpha) << 24);
			}
		}
		return true;
	}

	public bool GByteArrayToBitmap(Int64[,] array, int xstart, int ystart)
	{
		if (g == null || RealBitmap == null)
			throw new NullReferenceException();
		int w = RealBitmap.Width;
		int h = RealBitmap.Height;
		if (xstart + w > array.GetLength(0) || ystart + h > array.GetLength(1))
			return false;
		SKColor[] pixels = new SKColor[w * h];
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				Int64 argb = array[x + xstart, y + ystart];
				pixels[y * w + x] = new SKColor(
					(byte)((argb >> 16) & 0xFF), // R
					(byte)((argb >> 8) & 0xFF),  // G
					(byte)(argb & 0xFF),          // B
					(byte)((argb >> 24) & 0xFF)  // A
				);
			}
		}
		RealBitmap.Pixels = pixels;
		return true;
	}

	public void Load()
	{
		if (RealBitmap != null)
			return;
		if (drawImgList == null)
			return;
		RealBitmap = new SKBitmap(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
		g = new SKCanvas(RealBitmap);
		foreach (Tuple<ASprite, Rectangle> tuple in drawImgList)
			tuple.Item1.GraphicsDraw(g, tuple.Item2);
		AppContents.tempLoadedGraphicsImages.Add(this);
	}
	#endregion
}
