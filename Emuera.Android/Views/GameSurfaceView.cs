using Android.Content;
using Android.Views;
using SkiaSharp;
using SkiaSharp.Views.Android;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Platform;

namespace Emuera.Android.Views;

/// <summary>
/// SkiaSharp-backed canvas view that renders the ERA console display lines.
/// Touch events are translated to button clicks and scroll gestures.
/// </summary>
public class GameSurfaceView : SKCanvasView
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

    public GameSurfaceView(Context context) : base(context)
    {
    }

    /// <summary>Called after construction to break the circular dependency with EmueraConsole.</summary>
    public void SetConsole(EmueraConsole console)
    {
        _console = console;
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Black);
        DrawConsole(canvas, e.Info.Width, e.Info.Height);
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

        int bottomLine = Math.Min(scrollPos, lines.Count) - 1;

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
        // Determine alignment offset
        float x = startX;

        foreach (var button in line.Buttons)
        {
            float bx = button.PointX >= 0 ? button.PointX : x;
            float buttonStartX = bx;

            foreach (var part in button.StrArray)
            {
                if (part is ConsoleStyledString css)
                {
                    DrawStyledString(canvas, css, bx, lineY, button);
                    var argb = css.DisplayColor.ToArgb();
                    // Track button hit region
                    if (button.IsButton && css.Width > 0)
                    {
                        var bounds = new SKRect(bx, lineY, bx + css.Width, lineY + MinorShift.Emuera.Config.LineHeight);
                        _buttonRects.Add((bounds, button));
                    }
                    bx += css.Width;
                }
                else if (part is ConsoleImagePart)
                {
                    // Image rendering is a future task; skip width
                    bx += part.Width;
                }
                else
                {
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
        System.Drawing.Color sysColor = (_console.ButtonIsSelected(button) && button.IsButton)
            ? css.DisplayButtonColor
            : css.DisplayColor;

        _textPaint.Typeface = typeface;
        _textPaint.TextSize = font.SizeInPixels;
        _textPaint.Color = new SKColor(sysColor.R, sysColor.G, sysColor.B, sysColor.A);
        _textPaint.FakeBoldText = font.IsBold && typeface != null && !typeface.IsBold;

        // SkiaSharp draws text with baseline at y; add font size to move to correct position
        float baseline = y + font.SizeInPixels;
        canvas.DrawText(css.Str, x, baseline, _textPaint);

        // Underline / strikeout
        if (font.IsUnderline)
        {
            float underlineY = baseline + 2;
            canvas.DrawLine(x, underlineY, x + css.Width, underlineY, _textPaint);
        }
        if (font.IsStrikeout)
        {
            float strikeY = y + font.SizeInPixels / 2f;
            canvas.DrawLine(x, strikeY, x + css.Width, strikeY, _textPaint);
        }
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
        // Hit-test against button regions captured during last paint
        foreach (var (bounds, button) in _buttonRects)
        {
            if (bounds.Contains(x, y))
            {
                // Submit the button's input value to the console
                _console.PressEnterKey(false, button.Inputs, true);
                return;
            }
        }
    }

    /// <summary>Called by AndroidConsoleHost.ScrollToBottom to reset scroll offset.</summary>
    public void ResetScrollToBottom()
    {
        _scrollOffsetY = 0f;
        PostInvalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _textPaint.Dispose();
            foreach (var tf in _typefaceCache.Values)
                tf?.Dispose();
            _typefaceCache.Clear();
        }
        base.Dispose(disposing);
    }
}
