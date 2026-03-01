using Android.Content;
using MinorShift.Emuera.Platform;

namespace Emuera.Android.Platform;

/// <summary>
/// Android implementation of IPlatformPaths.
/// Stores game root in a caller-supplied URI path and uses internal app storage for saves/config.
/// </summary>
public class AndroidPlatformPaths(Context context, string gameRootDirectory) : IPlatformPaths
{
    public string GameRootDirectory { get; } = gameRootDirectory;

    public string SaveDirectory { get; } =
        Path.Combine(context.FilesDir!.AbsolutePath, "saves");

    public string ConfigFilePath { get; } =
        Path.Combine(context.FilesDir!.AbsolutePath, "emuera.config");
}
