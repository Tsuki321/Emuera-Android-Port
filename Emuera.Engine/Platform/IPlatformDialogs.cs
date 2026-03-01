namespace MinorShift.Emuera.Platform;

/// <summary>
/// Platform abstraction for modal dialogs. Replaces System.Windows.Forms.MessageBox.
/// </summary>
public interface IPlatformDialogs
{
    /// <summary>Show a yes/no dialog. Returns true if the user clicked Yes.</summary>
    Task<bool> ShowYesNoAsync(string message, string title);

    /// <summary>Show an informational dialog.</summary>
    Task ShowInfoAsync(string message, string title);
}
