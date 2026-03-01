using Android.Content;

namespace Emuera.Android.Platform;

/// <summary>
/// Writes unhandled exception details to a persistent log file in the app's internal storage.
/// Log location: &lt;FilesDir&gt;/logs/emuera_crash.log
/// </summary>
public static class CrashLogger
{
    private static string? _logPath;
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

            string logDir = Path.Combine(context.FilesDir!.AbsolutePath, "logs");
            Directory.CreateDirectory(logDir);
            _logPath = Path.Combine(logDir, "emuera_crash.log");
        }

        // Register global handlers after releasing the lock so they can call LogException freely.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogException((Exception)e.ExceptionObject, "AppDomain.UnhandledException");

        global::Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
        {
            LogException(e.Exception, "AndroidEnvironment.UnhandledExceptionRaiser");
            e.Handled = false;  // let the default handler terminate the process
        };
    }

    /// <summary>
    /// Appends the exception details (with a UTC timestamp) to the crash log file.
    /// Also writes to the debug output for convenience during development.
    /// </summary>
    public static void LogException(Exception ex, string? context = null)
    {
        string message = BuildEntry(ex, context);
        System.Diagnostics.Debug.WriteLine(message);

        if (_logPath is null)
            return;

        lock (_fileLock)
        {
            try
            {
                File.AppendAllText(_logPath, message);
            }
            catch
            {
                // If we cannot write the log file there is nothing safe left to do.
            }
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
