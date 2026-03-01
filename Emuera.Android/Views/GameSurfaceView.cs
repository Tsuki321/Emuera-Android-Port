using Android.Content;
using Android.Graphics;
using Android.Views;
using SkiaSharp;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Platform;

namespace Emuera.Android.Views;

/// <summary>
/// Custom Android View that renders the ERA console display lines using SkiaSharp.
/// Touch events are translated to button clicks and scroll gestures.
/// Renders by drawing to an off-screen SKBitmap then blitting to Android Canvas.
/// </summary>
public class GameSurfaceView : View
{
    private EmueraConsole? _console;

    // Scroll state
    private float _scrollOffsetY = 0f;
    private float _totalContentHeight = 0f;
    private float _lastTouchY = 0f;
    private bool _isScrolling = false;

    // Button hit regions (rebuilt each paint pass)
    private readonly List<(SKRect bounds, ConsoleButtonString button)> _buttonRects = [];

    // Font / paint cache
    private readonly Dictionary<(string family, float size, bool bold, bool italic), SKTypeface> _typefaceCache = [];
    private readonly SKPaint _textPaint = new() { IsAntialias = true };

    // Off-screen SkiaSharp surface
    private SKBitmap? _skBitmap;
    private SKCanvas? _skCanvas;
    private global::Android.Graphics.Bitmap? _androidBitmap;

    public GameSurfaceView(Context context) : base(context)
    {
    }

    /// <summary>Called after construction to break the circular dependency with EmueraConsole.</summary>
    internal void SetConsole(EmueraConsole console)
    {
        _console = console;
    }

    protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
    {
        base.OnSizeChanged(w, h, oldw, oldh);
        // Recreate off-screen bitmap when view size changes
        _skCanvas?.Dispose();
        _skBitmap?.Dispose();
        _androidBitmap?.Recycle();
        _androidBitmap?.Dispose();
        if (w > 0 && h > 0)
        {
            // BGRA8888 matches Android's ARGB_8888 byte order on little-endian ARM
            _skBitmap = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
            _skCanvas = new SKCanvas(_skBitmap);
            _androidBitmap = global::Android.Graphics.Bitmap.CreateBitmap(w, h, global::Android.Graphics.Bitmap.Config.Argb8888!);
        }
    }

    protected override void OnDraw(global::Android.Graphics.Canvas? canvas)
    {
        base.OnDraw(canvas);
        if (canvas == null || _skBitmap == null || _skCanvas == null || _androidBitmap == null) return;

        // Render ERA console into SkiaSharp bitmap
        _skCanvas.Clear(SKColors.Black);
        DrawConsole(_skCanvas, Width, Height);

        // Copy SkiaSharp pixels into the Android Bitmap
        using var byteBuffer = Java.Nio.ByteBuffer.Wrap(_skBitmap.Bytes)!;
        _androidBitmap.CopyPixelsFromBuffer(byteBuffer);

        canvas.DrawBitmap(_androidBitmap, 0, 0, null);
    }

    private void DrawConsole(SKCanvas canvas, int viewWidth, int viewHeight)
    {
        _buttonRects.Clear();

        if (_console == null) return;

        var lines = _console.DisplayLineList;
        if (lines == null || lines.Count == 0)
            return;

        int lineHeight = MinorShift.Emuera.Config.LineHeight;
        IConsoleHost? host = _console.Window;
        int scrollPos = host?.ScrollPosition ?? lines.Count;
        _totalContentHeight = lines.Count * lineHeight;

        // Apply user-initiated scroll: offset is pixels scrolled up from the engine's
        // natural bottom position.  Convert to line units (integer granularity).
        int userScrollLines = (int)(_scrollOffsetY / lineHeight);
        int bottomLine = Math.Min(scrollPos, lines.Count) - 1 - userScrollLines;
        bottomLine = Math.Max(0, bottomLine);

        // Walk lines upward from the bottom-most visible line
        float y = viewHeight - lineHeight;
        for (int i = bottomLine; i >= 0 && y > -lineHeight; i--)
        {
            ConsoleDisplayLine line = lines[i];
            DrawDisplayLine(canvas, line, 0, y, viewWidth);
            y -= lineHeight;
        }
    }

    private void DrawDisplayLine(SKCanvas canvas, ConsoleDisplayLine line, float startX, float lineY, int viewWidth)
    {
        float x = startX;

        foreach (var button in line.Buttons)
        {
            float bx = button.PointX >= 0 ? button.PointX : x;

            foreach (var part in button.StrArray)
            {
                if (part is ConsoleStyledString css)
                {
                    DrawStyledString(canvas, css, bx, lineY, button);
                    if (button.IsButton && css.Width > 0)
                    {
                        var bounds = new SKRect(bx, lineY, bx + css.Width, lineY + MinorShift.Emuera.Config.LineHeight);
                        _buttonRects.Add((bounds, button));
                    }
                    bx += css.Width;
                }
                else
                {
                    // ConsoleImagePart and others: skip rendering for now (future task)
                    bx += part.Width;
                }
            }

            x = bx;
        }
    }

    private void DrawStyledString(SKCanvas canvas, ConsoleStyledString css, float x, float y, ConsoleButtonString button)
    {
        if (string.IsNullOrEmpty(css.Str) || css.Error)
            return;

        EngineFont font = css.Font;
        var typeface = GetTypeface(font);

        // Choose color: use button color if this button is currently selected
        System.Drawing.Color sysColor = (_console!.ButtonIsSelected(button) && button.IsButton)
            ? css.DisplayButtonColor
            : css.DisplayColor;

        _textPaint.Typeface = typeface;
        _textPaint.TextSize = font.SizeInPixels;
        _textPaint.Color = new SKColor(sysColor.R, sysColor.G, sysColor.B, sysColor.A);
        _textPaint.FakeBoldText = font.IsBold && typeface != null && !typeface.IsBold;

        // SkiaSharp draws text with baseline at y; add font size to move to correct position
        float baseline = y + font.SizeInPixels;
        canvas.DrawText(css.Str, x, baseline, _textPaint);

        if (font.IsUnderline)
            canvas.DrawLine(x, baseline + 2, x + css.Width, baseline + 2, _textPaint);
        if (font.IsStrikeout)
            canvas.DrawLine(x, y + font.SizeInPixels / 2f, x + css.Width, y + font.SizeInPixels / 2f, _textPaint);
    }

    private SKTypeface GetTypeface(EngineFont font)
    {
        var key = (font.FamilyName, font.SizeInPixels, font.IsBold, font.IsItalic);
        if (_typefaceCache.TryGetValue(key, out var cached))
            return cached;

        var weight = font.IsBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        var slant  = font.IsItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
        var typeface = SKTypeface.FromFamilyName(font.FamilyName, weight, SKFontStyleWidth.Normal, slant);
        _typefaceCache[key] = typeface;
        return typeface;
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e == null) return base.OnTouchEvent(e);

        float x = e.GetX();
        float y = e.GetY();

        switch (e.Action)
        {
            case MotionEventActions.Down:
                _lastTouchY = y;
                _isScrolling = false;
                return true;

            case MotionEventActions.Move:
                float dy = y - _lastTouchY;
                if (Math.Abs(dy) > 8)
                {
                    _isScrolling = true;
                    _scrollOffsetY = Math.Clamp(_scrollOffsetY - dy,
                        0f, Math.Max(0f, _totalContentHeight - Height));
                    _lastTouchY = y;
                    PostInvalidate();
                }
                return true;

            case MotionEventActions.Up:
                if (!_isScrolling)
                    HandleTap(x, y);
                return true;
        }

        return base.OnTouchEvent(e);
    }

    private void HandleTap(float x, float y)
    {
        if (_console == null) return;
        foreach (var (bounds, button) in _buttonRects)
        {
            if (bounds.Contains(x, y))
            {
                _console.PressEnterKey(false, button.Inputs, true);
                return;
            }
        }
    }

    /// <summary>Called by AndroidConsoleHost.ScrollToBottom to reset scroll offset.</summary>
    public void ScrollToBottom()
    {
        _scrollOffsetY = 0f;
        PostInvalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _textPaint.Dispose();
            _skCanvas?.Dispose();
            _skBitmap?.Dispose();
            _androidBitmap?.Recycle();
            _androidBitmap?.Dispose();
            foreach (var tf in _typefaceCache.Values)
                tf?.Dispose();
            _typefaceCache.Clear();
        }
        base.Dispose(disposing);
    }
}

