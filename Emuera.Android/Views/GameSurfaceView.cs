using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Views.InputMethods;
using SkiaSharp;
using MinorShift.Emuera;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Platform;
using System.Runtime.InteropServices;

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
    private readonly Dictionary<(string? family, float size, bool bold, bool italic), SKTypeface> _typefaceCache = [];
    private readonly SKPaint _textPaint = new() { IsAntialias = true };

    // CJK fallback typeface — lazily resolved on first text draw
    private SKTypeface? _cjkFallback;
    private bool _cjkFallbackResolved;

    // Gesture detector for long-press copy
    private readonly GestureDetector _gestureDetector;

    // Off-screen SkiaSharp surface
    private SKBitmap? _skBitmap;
    private SKCanvas? _skCanvas;
    private global::Android.Graphics.Bitmap? _androidBitmap;

    // Cached pixel buffer to avoid per-frame byte[] allocation when blitting to Android.
    private byte[]? _pixelBuffer;

    public GameSurfaceView(Context context) : base(context)
    {
        _gestureDetector = new GestureDetector(context, new LongPressListener(this));
        LongClickable = true;
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
        _pixelBuffer = null;
        if (w > 0 && h > 0)
        {
            // BGRA8888 matches Android's ARGB_8888 byte order on little-endian ARM
            _skBitmap = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
            _skCanvas = new SKCanvas(_skBitmap);
            _androidBitmap = global::Android.Graphics.Bitmap.CreateBitmap(w, h, global::Android.Graphics.Bitmap.Config.Argb8888!);
            // Pre-allocate pixel buffer so OnDraw doesn't allocate per frame.
            _pixelBuffer = new byte[w * h * 4];
        }
    }

    protected override void OnDraw(global::Android.Graphics.Canvas? canvas)
    {
        base.OnDraw(canvas!);
        if (canvas == null || _skBitmap == null || _skCanvas == null || _androidBitmap == null) return;

        // Render ERA console into SkiaSharp bitmap
        _skCanvas.Clear(SKColors.Black);
        DrawConsole(_skCanvas, Width, Height);

        // Copy SkiaSharp pixels into the Android Bitmap.
        // IMPORTANT: Java.Nio.ByteBuffer.Wrap(byte[]) copies the C# array into a separate
        // Java-managed array at construction time, so the ByteBuffer must be created fresh
        // each frame from the updated _pixelBuffer — a pre-cached wrapper would always
        // contain the initial zeros, causing the screen to remain black on every frame.
        if (_pixelBuffer != null)
        {
            Marshal.Copy(_skBitmap.GetPixels(), _pixelBuffer, 0, _pixelBuffer.Length);
            using var byteBuffer = Java.Nio.ByteBuffer.Wrap(_pixelBuffer)!;
            _androidBitmap.CopyPixelsFromBuffer(byteBuffer);
        }
        else
        {
            // Fallback (should not normally be reached after OnSizeChanged).
            using var byteBuffer = Java.Nio.ByteBuffer.Wrap(_skBitmap.Bytes)!;
            _androidBitmap.CopyPixelsFromBuffer(byteBuffer);
        }

        canvas.DrawBitmap(_androidBitmap, 0, 0, null);
    }

    private void DrawConsole(SKCanvas canvas, int viewWidth, int viewHeight)
    {
        _buttonRects.Clear();

        if (_console == null) return;

        var lines = _console.DisplayLineList;
        if (lines == null || lines.Count == 0)
            return;

        // Clip all rendering to the view bounds so content never spills outside.
        canvas.ClipRect(new SKRect(0, 0, viewWidth, viewHeight));

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

        // Draw with per-character CJK fallback so Japanese glyphs render even when
        // the primary typeface (e.g. a Windows font name not found on Android) lacks them.
        DrawTextWithCjkFallback(canvas, css.Str, x, baseline, _textPaint);

        if (font.IsUnderline)
            canvas.DrawLine(x, baseline + 2, x + css.Width, baseline + 2, _textPaint);
        if (font.IsStrikeout)
            canvas.DrawLine(x, y + font.SizeInPixels / 2f, x + css.Width, y + font.SizeInPixels / 2f, _textPaint);
    }

    /// <summary>
    /// Draws <paramref name="text"/> using <paramref name="paint"/>'s current typeface for
    /// characters that are present in that face, and the CJK fallback typeface for any
    /// characters (e.g. Japanese kana/kanji) that the primary face does not have.
    /// Uses <see cref="System.Text.Rune"/> iteration to correctly handle surrogate pairs.
    /// </summary>
    private void DrawTextWithCjkFallback(SKCanvas canvas, string text, float x, float baseline, SKPaint paint)
    {
        var primary = paint.Typeface;
        var fallback = GetCjkFallback();

        // Fast path: primary font has all glyphs, or no fallback available.
        if (fallback == null || primary == null || primary.ContainsGlyphs(text))
        {
            canvas.DrawText(text, x, baseline, paint);
            return;
        }

        // Slow path: walk Unicode scalar values (Runes) so surrogate pairs are handled
        // correctly. Split into contiguous runs that all use the same font.
        float cx = x;
        int runStart = 0;       // char index in `text` where the current run began
        bool usingFallback = false;
        bool runStarted = false;

        void FlushRun(int endCharIdx)
        {
            if (!runStarted || endCharIdx <= runStart) return;
            string run = text.Substring(runStart, endCharIdx - runStart);
            paint.Typeface = usingFallback ? fallback : primary;
            canvas.DrawText(run, cx, baseline, paint);
            cx += paint.MeasureText(run);
            runStart = endCharIdx;
        }

        int charIdx = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            bool needsFallback = primary.GetGlyph(rune.Value) == 0;

            if (!runStarted)
            {
                usingFallback = needsFallback;
                runStarted = true;
            }
            else if (needsFallback != usingFallback)
            {
                FlushRun(charIdx);
                usingFallback = needsFallback;
            }

            charIdx += rune.Utf16SequenceLength;
        }
        FlushRun(charIdx); // flush the final run

        // Restore the paint typeface to primary so callers are unaffected.
        paint.Typeface = primary;
    }

    private SKTypeface GetTypeface(EngineFont font)
    {
        var key = (font.FamilyName, font.SizeInPixels, font.IsBold, font.IsItalic);
        if (_typefaceCache.TryGetValue(key, out var cached))
            return cached;

        var weight = font.IsBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        var slant  = font.IsItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

        SKTypeface? typeface = null;

        // 1. Check fonts the game placed in its font/ directory (e.g. NotoSansJP-Regular.ttf).
        if (!string.IsNullOrEmpty(font.FamilyName))
        {
            for (int i = 0; i < GlobalStatic.CustomFontFamilyNames.Count; i++)
            {
                if (GlobalStatic.CustomFontFamilyNames[i].Equals(
                        font.FamilyName, StringComparison.OrdinalIgnoreCase))
                {
                    typeface = SKTypeface.FromFile(GlobalStatic.CustomFontPaths[i]);
                    break;
                }
            }
        }

        // 2. Try system-installed fonts (works for "sans-serif", "serif", etc.).
        if (typeface == null && !string.IsNullOrEmpty(font.FamilyName))
            typeface = SKTypeface.FromFamilyName(font.FamilyName, weight, SKFontStyleWidth.Normal, slant);

        // 3. Fall back: use the best CJK-capable font we can find (user-supplied or system).
        typeface ??= GetCjkFallback();

        // 4. Absolute last resort — should never be reached.
        typeface ??= SKTypeface.FromFamilyName("sans-serif", weight, SKFontStyleWidth.Normal, slant)
                  ?? SKTypeface.Default;

        _typefaceCache[key] = typeface;
        return typeface;
    }

    /// <summary>
    /// Returns the best CJK-capable typeface available on this device, checking:
    /// <list type="number">
    ///   <item>Fonts supplied by the user in the game's <c>font/</c> directory.</item>
    ///   <item>Well-known Android system font paths (Noto Sans CJK, DroidSansFallback).</item>
    /// </list>
    /// The result is cached after the first call.
    /// </summary>
    private SKTypeface? GetCjkFallback()
    {
        if (_cjkFallbackResolved) return _cjkFallback;
        _cjkFallbackResolved = true;

        // Probe string covers hiragana, katakana and a common kanji.
        const string cjkProbe = "あアー字";

        // 1. User-supplied fonts from the game's font/ directory.
        for (int i = 0; i < GlobalStatic.CustomFontPaths.Count; i++)
        {
            try
            {
                var tf = SKTypeface.FromFile(GlobalStatic.CustomFontPaths[i]);
                if (tf != null && tf.ContainsGlyphs(cjkProbe))
                {
                    _cjkFallback = tf;
                    return _cjkFallback;
                }
                tf?.Dispose();
            }
            catch { }
        }

        // 2. Well-known Android system CJK font paths (vary by OEM and Android version).
        string[] systemCjkPaths =
        [
            "/system/fonts/NotoSansCJK-Regular.ttc",
            "/system/fonts/NotoSansCJKjp-Regular.otf",
            "/system/fonts/NotoSansJP-Regular.otf",
            "/system/fonts/DroidSansFallback.ttf",
            "/system/fonts/DroidSansFallbackFull.ttf",
        ];
        foreach (var path in systemCjkPaths)
        {
            if (!File.Exists(path)) continue;
            try
            {
                var tf = SKTypeface.FromFile(path);
                if (tf != null)
                {
                    _cjkFallback = tf;
                    return _cjkFallback;
                }
            }
            catch { }
        }

        return null;
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e == null) return base.OnTouchEvent(e);

        // Let the gesture detector handle long-press (copy to clipboard).
        _gestureDetector.OnTouchEvent(e);

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

    /// <summary>
    /// Collects the text currently visible on screen and copies it to the clipboard.
    /// Invoked on long-press via <see cref="LongPressListener"/>.
    /// </summary>
    internal void CopyVisibleTextToClipboard()
    {
        if (_console == null) return;

        var lines = _console.DisplayLineList;
        if (lines == null || lines.Count == 0) return;

        int lineHeight = MinorShift.Emuera.Config.LineHeight;
        IConsoleHost? host = _console.Window;
        int scrollPos = host?.ScrollPosition ?? lines.Count;
        int userScrollLines = (int)(_scrollOffsetY / lineHeight);
        int bottomLine = Math.Min(scrollPos, lines.Count) - 1 - userScrollLines;
        bottomLine = Math.Max(0, bottomLine);

        int visibleLines = Height / lineHeight + 1;
        int topLine = Math.Max(0, bottomLine - visibleLines + 1);

        var sb = new System.Text.StringBuilder();
        for (int i = topLine; i <= bottomLine; i++)
        {
            foreach (var button in lines[i].Buttons)
                foreach (var part in button.StrArray)
                    if (part is ConsoleStyledString css)
                        sb.Append(css.Str);
            sb.AppendLine();
        }

        var clipboard = (ClipboardManager?)Context?.GetSystemService(global::Android.Content.Context.ClipboardService);
        if (clipboard != null)
        {
            clipboard.PrimaryClip = ClipData.NewPlainText("Emuera", sb.ToString());
            // Brief toast confirmation
            global::Android.Widget.Toast.MakeText(Context, "Text copied", global::Android.Widget.ToastLength.Short)?.Show();
        }
    }

    /// <summary>GestureDetector listener that triggers text-copy on long-press.</summary>
    private sealed class LongPressListener(GameSurfaceView owner) : Java.Lang.Object, GestureDetector.IOnGestureListener
    {
        public bool OnDown(MotionEvent? e) => false;
        public bool OnFling(MotionEvent? e1, MotionEvent? e2, float vX, float vY) => false;
        public void OnLongPress(MotionEvent? e) => owner.CopyVisibleTextToClipboard();
        public bool OnScroll(MotionEvent? e1, MotionEvent? e2, float dX, float dY) => false;
        public void OnShowPress(MotionEvent? e) { }
        public bool OnSingleTapUp(MotionEvent? e) => false;
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
            _cjkFallback?.Dispose();
            foreach (var tf in _typefaceCache.Values)
                tf?.Dispose();
            _typefaceCache.Clear();
        }
        base.Dispose(disposing);
    }
}

