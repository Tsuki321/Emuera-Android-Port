namespace MinorShift.Emuera.GameView;

/// <summary>
/// Cross-platform stub for the debug dialog.
/// The WinForms implementation (Emuera/Forms/DebugDialog.cs) extends this.
/// On Android the no-op implementation is sufficient since debug tooling differs.
/// </summary>
public class DebugDialog
{
    /// <summary>Returns true if the dialog window is currently open and alive.</summary>
    public virtual bool Created => false;

    /// <summary>Associate this dialog with the engine console and script process.</summary>
    internal virtual void SetParent(EmueraConsole console, GameProc.Process process) { }

    /// <summary>Re-apply the current UI language translations to all visible labels.</summary>
    public virtual void TranslateUI() { }

    /// <summary>Bring the dialog window to the front.</summary>
    public virtual void Focus() { }

    /// <summary>Show the dialog window.</summary>
    public virtual void Show() { }

    /// <summary>Dispose the dialog and release its resources.</summary>
    public virtual void Dispose() { }
}
