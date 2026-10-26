using System.Security.Cryptography;
using System.Text;

namespace MinorShift.Emuera.Platform;

/// <summary>Stable storage identity, including games with identical folder names.</summary>
public static class GameStorage
{
    public static string GetGameId(string gameRoot)
    {
        string canonical = Path.GetFullPath(gameRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (OperatingSystem.IsWindows())
            canonical = canonical.ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
