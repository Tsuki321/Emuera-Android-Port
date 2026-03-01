namespace MinorShift.Emuera.GameView;

/// <summary>
/// Cross-platform stub for the Rikaichan index-generation dialog.
/// The WinForms implementation (Emuera/Forms/RikaiDialog.cs) extends this.
/// On Android the index generation is handled differently.
/// </summary>
public class RikaiDialogBase
{
    public RikaiDialogBase(string filename, byte[] data, Action<byte[]> indexCallback) { }

    /// <summary>Show the dialog window.</summary>
    public virtual void Show() { }

    /// <summary>Dispose the dialog and release its resources.</summary>
    public virtual void Dispose() { }
}
