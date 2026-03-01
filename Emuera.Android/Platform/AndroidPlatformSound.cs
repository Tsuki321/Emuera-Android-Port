using Android.Media;
using MinorShift.Emuera.Platform;

namespace Emuera.Android.Platform;

/// <summary>
/// Android implementation of IPlatformSound using MediaPlayer for BGM
/// and SoundPool for short sound effects.
/// </summary>
public class AndroidPlatformSound : IPlatformSound
{
    private MediaPlayer? _bgmPlayer;
    private SoundPool? _soundPool;
    private readonly Dictionary<string, int> _soundIds = [];

    public void PlayBGM(string filePath, bool loop)
    {
        StopBGM();
        _bgmPlayer?.Dispose();
        _bgmPlayer = new MediaPlayer();
        _bgmPlayer.SetDataSource(filePath);
        _bgmPlayer.Looping = loop;
        _bgmPlayer.Prepare();
        _bgmPlayer.Start();
    }

    public void StopBGM()
    {
        if (_bgmPlayer is { IsPlaying: true })
            _bgmPlayer.Stop();
    }

    public void PlaySE(string filePath)
    {
        EnsureSoundPool();
        if (!_soundIds.TryGetValue(filePath, out int soundId))
        {
            soundId = _soundPool!.Load(filePath, 1);
            _soundIds[filePath] = soundId;
        }
        _soundPool!.Play(soundId, 1f, 1f, 0, 0, 1f);
    }

    public void StopAll()
    {
        StopBGM();
        _soundPool?.AutoPause();
    }

    public void SetBGMVolume(int volume)
    {
        float v = Math.Clamp(volume / 100f, 0f, 1f);
        _bgmPlayer?.SetVolume(v, v);
    }

    public void Dispose()
    {
        _bgmPlayer?.Dispose();
        _bgmPlayer = null;
        _soundPool?.Dispose();
        _soundPool = null;
    }

    private void EnsureSoundPool()
    {
        if (_soundPool != null) return;
        var attrs = new AudioAttributes.Builder()!
            .SetUsage(AudioUsageKind.Game)!
            .SetContentType(AudioContentType.Sonification)!
            .Build()!;
        _soundPool = new SoundPool.Builder()!
            .SetMaxStreams(8)!
            .SetAudioAttributes(attrs)!
            .Build()!;
    }
}
