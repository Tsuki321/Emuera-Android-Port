using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using MinorShift.Emuera;
using MinorShift.Emuera.GameView;
using Emuera.Android.Platform;
using Emuera.Android.Views;
using SkiaSharp;

namespace Emuera.Android;

/// <summary>
/// Activity that runs the ERA game engine. Receives the game root path from MainActivity.
/// Creates the SkiaSharp surface, registers platform services, and boots the engine on a background thread.
/// </summary>
[Activity(Label = "Game", ScreenOrientation = global::Android.Content.PM.ScreenOrientation.Portrait,
          Theme = "@style/AppTheme.GameActivity")]
public class GameActivity : Activity
{
    private Thread? _engineThread;
    private EmueraConsole? _console;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        CrashLogger.Initialize(this);

        string? gameRoot = Intent?.GetStringExtra("GAME_ROOT");
        if (string.IsNullOrEmpty(gameRoot))
        {
            Finish();
            return;
        }

        // Register the game root with CrashLogger so that all subsequent exceptions
        // (including unhandled ones) are also logged to the game folder for easy access.
        CrashLogger.SetGameRootPath(gameRoot);

        // Register additional encodings (e.g. EUC-JP / code page 20932) which are
        // not available by default on Android/.NET.
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // 1. Register platform services in GlobalStatic
        GlobalStatic.Paths    = new AndroidPlatformPaths(this, gameRoot);
        GlobalStatic.Dialogs  = new AndroidPlatformDialogs(this);
        GlobalStatic.Sound    = new AndroidPlatformSound();
        GlobalStatic.Lifecycle = new AndroidLifecycle(this);

        // 2. Create the engine console host and surface view.
        //    Surface view is created here; EmueraConsole is created on the engine thread
        //    AFTER LoadConfig() so that Config.ForeColor/BackColor are set before the
        //    console's field initializers (defaultStyle, bgColor) execute.  Without this
        //    ordering those fields get Color.Empty (A=0, transparent) and all text is
        //    invisible on screen even though the text data is present and copyable.
        var surfaceView = new GameSurfaceView(this);
        var host = new AndroidConsoleHost(surfaceView);

        // 3. Build layout: console surface + input bar
        var rootLayout = new LinearLayout(this);
        rootLayout.Orientation = global::Android.Widget.Orientation.Vertical;

        var surfaceParams = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 0, 1f);
        rootLayout.AddView(surfaceView, surfaceParams);

        var inputBar = new InputBarView(this);
        // Wire the keyboard-show action so BeginWaitInput() opens the soft keyboard.
        host.ShowKeyboardAction = () => inputBar.RequestFocusForInput();
        var inputParams = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);
        rootLayout.AddView(inputBar, inputParams);

        SetContentView(rootLayout);

        // 4. Boot engine on a background thread
        _engineThread = new Thread(() =>
        {
            try
            {
                ConfigData.Instance.LoadConfig();
                // Discover TTF/OTF/TTC files in the game's font/ directory so the
                // engine can report them as "installed" and SkiaSharp can load them.
                LoadCustomFonts();
                // Create EmueraConsole after LoadConfig() so that Config.ForeColor and
                // Config.BackColor are set before the console's field initializers run.
                _console = new EmueraConsole(host);
                surfaceView.SetConsole(_console);
                inputBar.SetConsole(_console);
                _console.Initialize();
            }
            catch (Exception ex)
            {
                CrashLogger.LogException(ex, "Engine thread");
                RunOnUiThread(() => ShowEngineCrashDialog(ex, gameRoot));
            }
        });
        _engineThread.IsBackground = true;
        _engineThread.Name = "EmueraEngine";
        _engineThread.Start();
    }

    /// <summary>
    /// Displays an error dialog showing the engine crash details so the user
    /// can see what went wrong even when no text was printed to the console.
    /// </summary>
    private void ShowEngineCrashDialog(Exception ex, string gameRoot)
    {
        if (IsFinishing || IsDestroyed) return;
        string logHint = !string.IsNullOrEmpty(gameRoot)
            ? $"\n\nA crash log has been saved to:\n{gameRoot}/emuera_crash.log"
            : "\n\nA crash log has been saved to app storage.";
        new AlertDialog.Builder(this)!
            .SetTitle("Engine Error")!
            .SetMessage($"The game engine encountered an error:\n\n{ex.GetType().Name}: {ex.Message}{logHint}")!
            .SetPositiveButton("OK", (_, _) => Finish())!
            .SetCancelable(false)!
            .Show();
    }

    protected override void OnDestroy()
    {
        _console?.Quit();
        GlobalStatic.Reset();
        base.OnDestroy();
    }

    // ── Font discovery ────────────────────────────────────────────────────────

    /// <summary>
    /// Scans the game's <c>font/</c> directory for TTF/OTF/TTC files and registers
    /// them in <see cref="GlobalStatic.CustomFontPaths"/> / <see cref="GlobalStatic.CustomFontFamilyNames"/>
    /// so the engine can report them as "installed" and <see cref="Views.GameSurfaceView"/> can load them.
    /// </summary>
    private static void LoadCustomFonts()
    {
        var fontDir = MinorShift.Emuera.Program.FontDir;
        if (!Directory.Exists(fontDir)) return;

        foreach (var file in Directory.GetFiles(fontDir, "*.*", SearchOption.TopDirectoryOnly))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext != ".ttf" && ext != ".otf" && ext != ".ttc") continue;
            try
            {
                // Load temporarily just to read the family name, then dispose.
                var tf = SKTypeface.FromFile(file);
                if (tf != null)
                {
                    GlobalStatic.CustomFontPaths.Add(file);
                    GlobalStatic.CustomFontFamilyNames.Add(tf.FamilyName);
                    tf.Dispose();
                }
            }
            catch { /* skip unreadable font files */ }
        }
    }
}
