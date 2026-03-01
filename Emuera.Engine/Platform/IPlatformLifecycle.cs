namespace MinorShift.Emuera.Platform;

/// <summary>
/// Platform abstraction for application lifecycle control.
/// Replaces Application.Exit() and restart logic.
/// </summary>
public interface IPlatformLifecycle
{
    /// <summary>Signal the host to gracefully exit the application.</summary>
    void RequestExit();

    /// <summary>Signal the host to restart / reload the game engine.</summary>
    void RequestRestart();
}
