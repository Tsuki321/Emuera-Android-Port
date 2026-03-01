using System;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;
using MinorShift.Emuera.Platform;
// Replaced GDI+ (Graphics/Bitmap/TextRenderer) with SkiaSharp for cross-platform text measurement.

namespace MinorShift.Emuera.GameView;

/// <summary>
/// テキスト長計測装置 (SkiaSharp cross-platform implementation)
/// </summary>
internal sealed class StringMeasure : IDisposable
{
	// Cache SKPaint per EngineFont to avoid recreation overhead
	private readonly Dictionary<EngineFont, SKPaint> paintCache = [];

	public StringMeasure()
	{
	}

	private SKPaint GetOrCreatePaint(EngineFont font)
	{
		if (paintCache.TryGetValue(font, out var cached))
			return cached;

		var typeface = SKTypeface.FromFamilyName(
			font.FamilyName,
			(font.Style & EngineFontStyle.Bold) != 0 ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
			SKFontStyleWidth.Normal,
			(font.Style & EngineFontStyle.Italic) != 0 ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

		var paint = new SKPaint
		{
			Typeface = typeface,
			TextSize = font.SizeInPixels,
			IsAntialias = true,
		};
		paintCache[font] = paint;
		return paint;
	}

	public int GetDisplayLength(string s, EngineFont font)
	{
		if (string.IsNullOrEmpty(s))
			return 0;
		if (s.Contains('\t'))
			s = s.Replace("\t", "        ");
		SKPaint paint = GetOrCreatePaint(font);
		return (int)Math.Ceiling(paint.MeasureText(s));
	}

	bool disposed = false;
	public void Dispose()
	{
		if (disposed) return;
		disposed = true;
		foreach (var p in paintCache.Values)
			p.Dispose();
		paintCache.Clear();
	}
}

