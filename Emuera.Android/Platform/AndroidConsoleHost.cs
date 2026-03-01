using Android.App;
using MinorShift.Emuera.Platform;
using Emuera.Android.Views;

namespace Emuera.Android.Platform;

/// <summary>
/// Android implementation of IConsoleHost.
/// Bridges the ERA engine console model to the SkiaSharp GameSurfaceView.
/// </summary>
public class AndroidConsoleHost : IConsoleHost
{
    private readonly GameSurfaceView _view;
    private string _windowTitle = "Emuera";
    private string _inputBoxText = "";
    private bool _inputBoxPositionChanged = false;
    private int _scrollPosition = 0;
    private int _maxScrollPosition = 0;

    public event Action<string>? InputSubmitted;

    public AndroidConsoleHost(GameSurfaceView view)
    {
        _view = view;
    }

    // ── IConsoleHost ──────────────────────────────────────────────────────────

    public bool IsAvailable => true;

    public int ConsoleWidth => _view.Width > 0 ? _view.Width : MinorShift.Emuera.Config.WindowX;
    public int ConsoleHeight => _view.Height > 0 ? _view.Height : MinorShift.Emuera.Config.LineHeight * 30;

    /// <summary>
    /// Optional action invoked by <see cref="BeginWaitInput"/> to show the soft keyboard.
    /// Wired by <see cref="GameActivity"/> after construction.
    /// </summary>
    public Action? ShowKeyboardAction { get; set; }

    public int ScrollPosition
    {
        get => _scrollPosition;
        set => _scrollPosition = value;
    }

    public int MaxScrollPosition
    {
        get => _maxScrollPosition;
        set => _maxScrollPosition = value;
    }

    public void RequestRedraw()
    {
        _view.PostInvalidate();
    }

    public void BeginWaitInput()
    {
        // Ask the platform to focus the text field and raise the soft keyboard.
        ShowKeyboardAction?.Invoke();
    }

    public void ScrollToBottom()
    {
        _view.ScrollToBottom();
    }

    public void SetWindowTitle(string title)
    {
        _windowTitle = title;
        // If the activity is available, update its title on the UI thread.
        // The activity reference is managed by GameActivity via WeakReference if needed.
    }

    public string GetWindowTitle() => _windowTitle;

    public string InputBoxText
    {
        get => _inputBoxText;
        set => _inputBoxText = value ?? "";
    }

    public bool InputBoxPositionChanged => _inputBoxPositionChanged;

    public void ResetInputBoxPosition()
    {
        _inputBoxPositionChanged = false;
    }

    public void SetInputBoxBackColor(System.Drawing.Color color)
    {
        // Future: update EditText background color on the UI thread
    }

    public void ShowConfigDialog()
    {
        // Future: show an Android settings screen
    }

    public void SetTooltip(string text)
    {
        // Tooltips are not used on Android (touch-first UI)
    }

    public System.Drawing.Point? GetConsoleRelativeCursorPosition()
    {
        // Touch position tracking is handled in GameSurfaceView.OnTouchEvent
        return null;
    }

    // ── Called from Android UI ────────────────────────────────────────────────

    /// <summary>
    /// Called by GameActivity input bar when the user submits text or selects a number.
    /// Fires the InputSubmitted event and also notifies engine directly.
    /// </summary>
    public void SubmitInput(string value)
    {
        InputSubmitted?.Invoke(value);
    }
}
