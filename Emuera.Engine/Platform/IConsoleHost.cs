namespace MinorShift.Emuera.Platform;

/// <summary>
/// Platform abstraction for the console rendering surface, input delivery, and UI controls.
/// Replaces MainWindow + EmueraConsole's WinForms rendering surface.
/// </summary>
public interface IConsoleHost
{
    /// <summary>Called by the engine when the display model has changed and needs a repaint.</summary>
    void RequestRedraw();

    /// <summary>Called when the engine enters input-wait state.</summary>
    void BeginWaitInput();

    /// <summary>Fired by the platform when the user submits input (text or number).</summary>
    event Action<string> InputSubmitted;

    /// <summary>Usable console draw width in pixels.</summary>
    int ConsoleWidth { get; }

    /// <summary>Usable console draw height in pixels.</summary>
    int ConsoleHeight { get; }

    /// <summary>Scroll the viewport to the bottom of the display list.</summary>
    void ScrollToBottom();

    // ── State ──────────────────────────────────────────────────────────────────

    /// <summary>True if the console surface is ready to accept draw calls.</summary>
    bool IsAvailable { get; }

    // ── Scroll position ────────────────────────────────────────────────────────

    /// <summary>
    /// Current scroll position (line index of the bottom-most visible line + 1).
    /// Replaces WinForms ScrollBar.Value.
    /// </summary>
    int ScrollPosition { get; set; }

    /// <summary>
    /// Maximum scroll value (total number of display lines).
    /// Replaces WinForms ScrollBar.Maximum.
    /// </summary>
    int MaxScrollPosition { get; set; }

    // ── Window chrome ──────────────────────────────────────────────────────────

    /// <summary>Sets the window/app title string.</summary>
    void SetWindowTitle(string title);

    /// <summary>Gets the current window/app title string.</summary>
    string GetWindowTitle();

    // ── Input box ─────────────────────────────────────────────────────────────

    /// <summary>Text currently typed in the input box.</summary>
    string InputBoxText { get; set; }

    /// <summary>
    /// True if the platform wants the engine to re-position the input box.
    /// Replaces WinForms MainWindow.TextBoxPosChanged.
    /// </summary>
    bool InputBoxPositionChanged { get; }

    /// <summary>Acknowledge that the engine has processed the input box position change.</summary>
    void ResetInputBoxPosition();

    /// <summary>Background color for the input text box (mapped from bgColor).</summary>
    void SetInputBoxBackColor(System.Drawing.Color color);

    // ── Dialogs / Config ───────────────────────────────────────────────────────

    /// <summary>Shows the engine configuration dialog (platform-specific UI).</summary>
    void ShowConfigDialog();

    // ── Tooltip ────────────────────────────────────────────────────────────────

    /// <summary>Display a tooltip near the cursor with the given text. Pass null to hide.</summary>
    void SetTooltip(string text);

    // ── Mouse / hit-test ───────────────────────────────────────────────────────

    /// <summary>Returns the current cursor position relative to the console surface, or null if outside.</summary>
    System.Drawing.Point? GetConsoleRelativeCursorPosition();
}
