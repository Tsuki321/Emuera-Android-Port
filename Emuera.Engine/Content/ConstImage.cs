using MinorShift._Library;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using SkiaSharp;

namespace MinorShift.Emuera.Content;

internal abstract class AbstractImage : AContentFile
{
	public const int MAX_IMAGESIZE = 8192;
	public abstract SKBitmap Bitmap { get; set; }
}

internal sealed class ConstImage : AbstractImage
{
	public ConstImage(string name)
	{ Name = name; RealIsCreated = false; }

	public readonly string Name;
	public SKBitmap RealBitmap;
	public string Filepath;
	public int Width;
	public int Height;
	public bool RealIsCreated;

	internal void CreateFrom(SKBitmap bmp, string filepath)
	{
		if (RealBitmap != null || !string.IsNullOrEmpty(Filepath))
			throw new Exception();
		try
		{
			RealBitmap = bmp;
			Filepath = filepath;
			Width = RealBitmap.Width;
			Height = RealBitmap.Height;
			AppContents.tempLoadedConstImages.Add(this);
			RealIsCreated = true;
		}
		catch
		{
			return;
		}
		return;
	}

	public void Load()
	{
		if (RealBitmap != null || !RealIsCreated)
			return;
		try
		{
			RealBitmap = ImgUtils.LoadImage(Filepath);
			if (RealBitmap == null)
				return;
			AppContents.tempLoadedConstImages.Add(this);
		}
		catch
		{
			return;
		}
		return;
	}

	public override void Dispose()
	{
		if (RealBitmap == null || !RealIsCreated)
			return;
		if (RealBitmap != null)
		{
			RealBitmap.Dispose();
			RealBitmap = null;
		}
	}

	~ConstImage()
	{
		Dispose();
	}

	public override bool IsCreated
	{
		get { return RealIsCreated; }
	}

	public override SKBitmap Bitmap
	{
		set { RealBitmap = value; }
		get
		{
			Load();
			return RealBitmap;
		}
	}
}
