namespace MinorShift.Emuera.Platform;

/// <summary>
/// Opaque handle to a platform-specific image resource (e.g. SKBitmap on Android).
/// Replaces System.Drawing.Bitmap in the engine data model.
/// </summary>
public interface IImageHandle : IDisposable
{
    int Width { get; }
    int Height { get; }
    bool IsValid { get; }
}
