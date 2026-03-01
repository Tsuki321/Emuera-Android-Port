using System;
using System.IO;
using SkiaSharp;

namespace MinorShift.Emuera.Content;

static class ImgUtils
{
	/// <summary>
	/// Load an image from disk using SkiaSharp (supports JPEG, PNG, BMP, WebP, GIF, etc.).
	/// Returns null if the file does not exist or cannot be decoded.
	/// </summary>
	public static SKBitmap LoadImage(string filepath)
	{
		if (!File.Exists(filepath))
			return null;
		try
		{
			return SKBitmap.Decode(filepath);
		}
		catch
		{
			return null;
		}
	}
}
