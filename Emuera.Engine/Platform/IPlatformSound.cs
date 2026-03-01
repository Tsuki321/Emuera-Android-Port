namespace MinorShift.Emuera.Platform;

/// <summary>
/// Platform abstraction for audio playback.
/// Replaces Sound.WMP.cs / Sound.NAudio.cs.
/// </summary>
public interface IPlatformSound : IDisposable
{
    void PlayBGM(string filePath, bool loop);
    void PlaySE(string filePath);
    void StopBGM();
    void StopAll();
    /// <param name="volume">0–100</param>
    void SetBGMVolume(int volume);
}
