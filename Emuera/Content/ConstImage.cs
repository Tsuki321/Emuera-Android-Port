using MinorShift._Library;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MinorShift.Emuera.Content;

internal abstract class AbstractImage : AContentFile
{
	public const int MAX_IMAGESIZE = 8192;
	public abstract Bitmap Bitmap { get; set; }
	public IntPtr GDIhDC { get; protected set; }
	protected Graphics g;
	protected IntPtr hBitmap;
	protected IntPtr hDefaultImg;
	protected bool gdi;
}

internal sealed class ConstImage : AbstractImage
{
	public ConstImage(string name)
	{ Name = name; RealIsCreated = false; }

	public readonly string Name;
	public Bitmap RealBitmap;
	public string Filepath;
	public int Width;
	public int Height;
	public bool RealIsCreated;

	internal void CreateFrom(Bitmap bmp, string filepath, bool useGDI)
	{
		if (RealBitmap != null || !string.IsNullOrEmpty(Filepath))
			throw new Exception();
		try
		{
			RealBitmap = bmp;
			Filepath = filepath;
			Width = RealBitmap.Width;
			Height = RealBitmap.Height;
			if (useGDI)
			{
				gdi = true;
				hBitmap = RealBitmap.GetHbitmap();
				g = Graphics.FromImage(RealBitmap);
				GDIhDC = g.GetHdc();
				hDefaultImg = GDI.SelectObject(GDIhDC, hBitmap);
			}

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
			{
				return;
			}

			if (gdi)
			{
				hBitmap = RealBitmap.GetHbitmap();
				g = Graphics.FromImage(RealBitmap);
				GDIhDC = g.GetHdc();
				hDefaultImg = GDI.SelectObject(GDIhDC, hBitmap);
			}
			AppContents.tempLoadedConstImages.Add(this);
		}
		catch
		{
			return;
		}
		return;
	}
	//public void Load(bool useGDI)
	//{
	//	if (Loaded)
	//		return;
	//	try
	//	{
	//		Bitmap = new Bitmap(Filepath);
	//		if (useGDI)
	//		{
	//			hBitmap = Bitmap.GetHbitmap();
	//			g = Graphics.FromImage(Bitmap);
	//			GDIhDC = g.GetHdc();
	//			hDefaultImg = GDI.SelectObject(GDIhDC, hBitmap);
	//		}
	//		Loaded = true;
	//		Enabled = true;
	//	}
	//	catch
	//	{
	//		return;
	//	}
	//	return;
	//}

	public override void Dispose()
	{
		if (RealBitmap == null || !RealIsCreated)
			return;
		if (gdi)
		{
			GDI.SelectObject(GDIhDC, hDefaultImg);
			GDI.DeleteObject(hBitmap);
			//gがすでに死んでると例外になる
			if (g != null)
				g.ReleaseHdc(GDIhDC);
		}
		if (g != null)
		{
			g.Dispose();
			g = null;
		}
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
		get
		{
			return RealIsCreated;
		}
	}

	public override Bitmap Bitmap
	{
		set
		{
			RealBitmap = value;
		}
		get
		{
			Load();
			return RealBitmap;
		}
	}
}
