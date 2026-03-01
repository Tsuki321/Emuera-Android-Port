namespace MinorShift.Emuera.Platform;

/// <summary>
/// Platform abstraction for a periodic callback timer.
/// Replaces System.Windows.Forms.Timer.
/// </summary>
public interface IPlatformTimer : IDisposable
{
    void Start(int intervalMs, Action callback);
    void Stop();
    bool IsRunning { get; }
}
