using Android.Content;

namespace Emuera.Android.Platform;

/// <summary>
/// Writes unhandled exception details to a persistent log file in the app's internal
/// storage, the publicly accessible external app directory, and optionally the game
/// root folder so users can easily locate the log alongside their game files.
/// Internal:   &lt;FilesDir&gt;/logs/emuera_crash.log
/// External:   Android/data/&lt;package&gt;/files/logs/emuera_crash.log
/// Game root:  &lt;gameRoot&gt;/emuera_crash.log  (set via <see cref="SetGameRootPath"/>)
/// </summary>
public static class CrashLogger
{
    private static string? _internalLogPath;
    private static string? _externalLogPath;
    private static string? _gameRootLogPath;
    private static readonly object _fileLock = new();
    private static bool _initialized = false;

    /// <summary>
    /// Must be called once (e.g. in MainActivity.OnCreate or GameActivity.OnCreate)
    /// before logging can occur. Safe to call multiple times – only the first call takes effect.
    /// Also registers global unhandled-exception handlers so crashes outside the engine
    /// thread are captured as well.
    /// </summary>
    public static void Initialize(Context context)
    {
        lock (_fileLock)
        {
            if (_initialized)
                return;
            _initialized = true;

            // Internal storage (always available, requires adb to read)
            string internalLogDir = Path.Combine(context.FilesDir!.AbsolutePath, "logs");
            Directory.CreateDirectory(internalLogDir);
            _internalLogPath = Path.Combine(internalLogDir, "emuera_crash.log");

            // External app-specific storage: Android/data/<package>/files/logs/
            // No runtime permission needed (app-private external dir, Android 4.4+).
            var externalDir = context.GetExternalFilesDir(null);
            if (externalDir != null)
            {
                string externalLogDir = Path.Combine(externalDir.AbsolutePath, "logs");
                Directory.CreateDirectory(externalLogDir);
                _externalLogPath = Path.Combine(externalLogDir, "emuera_crash.log");
            }
        }

        // Register global handlers after releasing the lock so they can call LogException freely.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogException((Exception)e.ExceptionObject, "AppDomain.UnhandledException");

        global::Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
        {
            LogException(e.Exception, "AndroidEnvironment.UnhandledExceptionRaiser");
            e.Handled = false;  // let the default handler terminate the process
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogException(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();  // prevent the process from being terminated
        };
    }

    /// <summary>
    /// Sets the game root directory so that crash logs are also written there,
    /// making them easy for users to find alongside their game files.
    /// Call this once the game root path is known (e.g. in GameActivity after
    /// the game root is received from the intent).
    /// </summary>
    public static void SetGameRootPath(string gameRootDirectory)
    {
        if (string.IsNullOrEmpty(gameRootDirectory))
            return;
        lock (_fileLock)
        {
            // Write directly to the game root folder so users can find the log easily.
            _gameRootLogPath = Path.Combine(gameRootDirectory, "emuera_crash.log");
        }
    }

    /// <summary>
    /// Appends the exception details (with a UTC timestamp) to all crash log files
    /// (internal storage, external app storage, and game root folder if set).
    /// Also writes to the debug output for convenience during development.
    /// </summary>
    public static void LogException(Exception ex, string? context = null)
    {
        string message = BuildEntry(ex, context);
        System.Diagnostics.Debug.WriteLine(message);

        lock (_fileLock)
        {
            WriteToFile(_internalLogPath, message);
            WriteToFile(_externalLogPath, message);
            WriteToFile(_gameRootLogPath, message);
        }
    }

    private static void WriteToFile(string? path, string message)
    {
        if (path is null)
            return;
        try
        {
            File.AppendAllText(path, message);
        }
        catch
        {
            // If we cannot write the log file there is nothing safe left to do.
        }
    }

    private static string BuildEntry(Exception ex, string? context)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("========================================");
        sb.Append("Timestamp : ").AppendLine(DateTime.UtcNow.ToString("o"));
        if (!string.IsNullOrEmpty(context))
            sb.Append("Context   : ").AppendLine(context);
        sb.AppendLine(ex.ToString());
        sb.AppendLine();
        return sb.ToString();
    }
}
