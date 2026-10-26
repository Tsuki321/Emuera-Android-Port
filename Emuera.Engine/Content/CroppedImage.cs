using MinorShift.Emuera.Sub;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using trerror = EvilMask.Emuera.Lang.Error;
using SkiaSharp;

namespace MinorShift.Emuera.Content;

abstract class AContentItem
{
	protected AContentItem(string name) { Name = name; }
	public readonly string Name;
	//public bool Enabled { get; protected set; }
	public abstract bool IsCreated { get; }
}

internal abstract class ASprite : AContentItem, IDisposable
{
	public ASprite(string name, Size size)
		: base(name)
	{
		if (size.Width < 0)
			size.Width = -size.Width;
		if (size.Height < 0)
			size.Height = -size.Height;
		DestBaseSize = size;
	}
	public abstract Color SpriteGetColor(int x, int y);
	/// <summary>
	/// 出力される標準のサイズ。正の値のみ。
	/// </summary>
	public readonly Size DestBaseSize;

	/// <summary>
	/// 出力時の位置調整。拡大縮小して出力する場合には同じ比率で調整する。
	/// </summary>
	public Point DestBasePosition;


	public abstract void GraphicsDraw(SKCanvas canvas, Point offset);
	public abstract void GraphicsDraw(SKCanvas canvas, Rectangle destRect);
	public abstract void GraphicsDraw(SKCanvas canvas, Rectangle destRect, object attr);
	public abstract void Dispose();
	public void Move(Point point) { DestBasePosition.Offset(point); }
}


internal abstract class ASpriteSingle : ASprite
{
	public ASpriteSingle(string name, AbstractImage img, Rectangle rect)
		: base(name, rect.Size)
	{
		SrcRectangle = rect;
		BaseImage = img;
	}
	public ASpriteSingle(string name, AbstractImage img, Rectangle rect, Size destSize)
		: base(name, destSize)
	{
		SrcRectangle = rect;
		BaseImage = img;
	}
	public AbstractImage BaseImage;

	/// <summary>
	/// ソース画像上の位置を指定する四角形。Width, Heightは負の値をとり得る
	/// </summary>
	public readonly Rectangle SrcRectangle;
	private SKBitmap Bitmap
	{
		get
		{
			if (BaseImage != null && BaseImage.IsCreated)
				return BaseImage.Bitmap;
			return null;
		}
	}

	public override bool IsCreated
	{
		get { return BaseImage != null && BaseImage.IsCreated; }
	}
	public override Color SpriteGetColor(int x, int y)
	{
		SKBitmap bmp = this.Bitmap;
		if (bmp == null)
			return Color.Transparent;
		int bmpX = x + SrcRectangle.X;
		int bmpY = y + SrcRectangle.Y;
		if (bmpX < 0 || bmpX >= bmp.Width || bmpY < 0 || bmpY >= bmp.Height)
			return Color.Transparent;
		SKColor sk = bmp.GetPixel(bmpX, bmpY);
		return Color.FromArgb(sk.Alpha, sk.Red, sk.Green, sk.Blue);
	}
	public override void Dispose()
	{
		BaseImage = null;
	}


	public override void GraphicsDraw(SKCanvas canvas, Point offset)
	{
		GraphicsDraw(canvas, new Rectangle(offset, DestBaseSize));
	}
	public override void GraphicsDraw(SKCanvas canvas, Rectangle destRect)
	{
		GraphicsDraw(canvas, destRect, null);
	}

	public override void GraphicsDraw(SKCanvas canvas, Rectangle destRect, object attr)
	{
		SKBitmap bitmap = Bitmap;
		if (bitmap == null || destRect.Width == 0 || destRect.Height == 0) return;
		canvas.Save();
		try
		{
			canvas.Translate(destRect.X, destRect.Y);
			canvas.Scale((float)destRect.Width / DestBaseSize.Width, (float)destRect.Height / DestBaseSize.Height);
			canvas.Translate(DestBasePosition.X, DestBasePosition.Y);
			var source = new SKRect(Math.Min(SrcRectangle.Left, SrcRectangle.Right), Math.Min(SrcRectangle.Top, SrcRectangle.Bottom),
				Math.Max(SrcRectangle.Left, SrcRectangle.Right), Math.Max(SrcRectangle.Top, SrcRectangle.Bottom));
			if (SrcRectangle.Width < 0) { canvas.Translate(DestBaseSize.Width, 0); canvas.Scale(-1, 1); }
			if (SrcRectangle.Height < 0) { canvas.Translate(0, DestBaseSize.Height); canvas.Scale(1, -1); }
			canvas.DrawBitmap(bitmap, source, new SKRect(0, 0, DestBaseSize.Width, DestBaseSize.Height), attr as SKPaint);
		}
		finally { canvas.Restore(); }
	}

}

/// <summary>
/// ERB中で作るGを元にしたSprite。GDI非対応
/// </summary>
internal sealed class SpriteG : ASpriteSingle
{
	public SpriteG(string name, GraphicsImage gra, Rectangle rect)
		: base(name, gra, rect)
	{
	}
	public bool useImgList { get { return (BaseImage as GraphicsImage).useImgList; } }
	public List<Tuple<ASprite, Rectangle>> drawImgList { get { return (BaseImage as GraphicsImage).drawImgList; } }
	public bool isBaseImage(GraphicsImage gImg)
	{
		return (BaseImage as GraphicsImage) == gImg;
	}

}

/// <summary>
/// ConstImage(csvから作るファイル占有型ベースイメージ)をもとにしたSprite
/// </summary>
internal sealed class SpriteF : ASpriteSingle
{
	public SpriteF(string name, ConstImage image, Rectangle rect, Point pos, Size destSize)
		: base(name, image, rect, destSize)
	{
		this.DestBasePosition = pos;
	}
}

/// <summary>
/// AnimeするSprite。中身はほぼSprite
/// </summary>
internal sealed class SpriteAnime : ASprite
{
	public SpriteAnime(string name, Size size)
		: base(name, size)
	{
		FrameList = [];
		totaltime = 0;
	}
	private sealed class AnimeFrame : IDisposable
	{
		public int index;
		public AbstractImage BaseImage;
		public Rectangle SrcRectangle;
		public Point Offset;
		public int DelayTimeMs;
		public void Normalize(Size parentSize)
		{
			Rectangle rect = Rectangle.Intersect(new Rectangle(Offset, SrcRectangle.Size), new Rectangle(new Point(), parentSize));
			if (rect.IsEmpty)
			{
				BaseImage = null;
				return;
			}
			Offset.X = rect.X;
			Offset.Y = rect.Y;
			SrcRectangle.Width = rect.Width;
			SrcRectangle.Height = rect.Height;
		}
		public void Dispose()
		{
			BaseImage = null;
		}
	}
	List<AnimeFrame> FrameList;
	public Int64 totaltime;

	internal bool AddFrame(AbstractImage parentImage, Rectangle rect, Point pos, int delay)
	{
		AnimeFrame frame = new()
		{
			index = FrameList.Count,
			BaseImage = parentImage,
			SrcRectangle = rect,
			Offset = pos
		};

		if (delay <= 0)
			delay = 1;
		frame.DelayTimeMs = delay;
		frame.Normalize(DestBaseSize);
		totaltime += delay;
		FrameList.Add(frame);
		return true;
	}

	/// <summary>
	/// アニメの経過時間を削除して最初からやり直す
	/// </summary>
	internal void ResetTime()
	{
		StartTime = -1;
		lastFrameTime = 0;
		lastFrame = -1;
	}

	/// <summary>
	/// 開始時間調整用の値。ミリ秒でUInt32の範囲まで想定。
	/// </summary>
	Int64 StartTime = -1;
	long lastFrameTime = 0;
	int lastFrame = -1;
	private AnimeFrame GetCurrentFrame()
	{
		if (totaltime <= 0)
			return null;
#if DEBUG
		if (FrameList.Count == 0)
			throw new ExeEE(trerror.EmptyFramelist.Text);
		if (lastFrame >= FrameList.Count)
			throw new ExeEE(trerror.OoRLasframe.Text);
#endif
		//一度もフレーム取得したことがない場合は現在時間を記録して最初のフレームを返す。
		if (StartTime < 0)
		{
			StartTime = Environment.TickCount64;
			lastFrame = 0;
			return FrameList[0];
		}
		//時間経過なしに複数回呼ばれた場合はさっき返したフレームをもう一度返す。
		if (Environment.TickCount64 == lastFrameTime && lastFrame >= 0)
			return FrameList[lastFrame];
		//StartTimeからの経過時間をtotaltimeで剰余計算
		var time = (Environment.TickCount64 - StartTime) % totaltime;      //winmmtimerは一周して0になることがあり得るのでその場合の対策。C#の剰余の結果の符号は左辺値の符号に等しい。
		if (time < 0)
			time += totaltime;
		foreach (AnimeFrame frame in FrameList)
		{
			time -= frame.DelayTimeMs;
			if (time <= 0)
			{
				lastFrame = frame.index;
				return frame;
			}
		}
		//ここまでこないはず
		throw new ExeEE(trerror.SpriteTimeOut.Text);
	}

	public override bool IsCreated
	{
		get { return true; }
	}

	public override void Dispose()
	{
		foreach (var frame in FrameList)
			frame.Dispose();
		FrameList.Clear();
		totaltime = 0;
		lastFrameTime = 0;
		StartTime = -1;
		lastFrame = -1;
	}


	public override Color SpriteGetColor(int x, int y)
	{
		throw new NotSupportedException();
		//Bitmap bmp = this.Bitmap;
		//if (bmp == null)
		//	return Color.Transparent;
		//int bmpX = x + SrcRectangle.X;
		//int bmpY = y + SrcRectangle.Y;
		//if (bmpX < 0 || bmpX >= bmp.Width || bmpY < 0 || bmpY >= bmp.Height)
		//	return Color.Transparent;

		//return bmp.GetPixel(bmpX, bmpY);
	}


	public override void GraphicsDraw(SKCanvas canvas, Point offset)
	{
		GraphicsDraw(canvas, new Rectangle(offset, DestBaseSize));
	}

	public override void GraphicsDraw(SKCanvas canvas, Rectangle destRect)
	{
		GraphicsDraw(canvas, destRect, null);
	}

	public override void GraphicsDraw(SKCanvas canvas, Rectangle destRect, object attr)
	{
		var frame = GetCurrentFrame();
		if (frame?.BaseImage?.Bitmap == null) return;
		using var sprite = new SpriteG("", frame.BaseImage as GraphicsImage, frame.SrcRectangle);
		sprite.BaseImage = frame.BaseImage;
		canvas.Save();
		try
		{
			canvas.Translate(destRect.X, destRect.Y);
			canvas.Scale((float)destRect.Width / DestBaseSize.Width, (float)destRect.Height / DestBaseSize.Height);
			sprite.GraphicsDraw(canvas, new Rectangle(frame.Offset.X + DestBasePosition.X, frame.Offset.Y + DestBasePosition.Y,
				frame.SrcRectangle.Width, frame.SrcRectangle.Height), attr);
		}
		finally { canvas.Restore(); }
	}

}
