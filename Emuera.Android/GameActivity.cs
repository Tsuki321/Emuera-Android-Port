using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using MinorShift.Emuera;
using MinorShift.Emuera.GameView;
using Emuera.Android.Platform;
using Emuera.Android.Views;

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

        // 1. Register platform services in GlobalStatic
        GlobalStatic.Paths    = new AndroidPlatformPaths(this, gameRoot);
        GlobalStatic.Dialogs  = new AndroidPlatformDialogs(this);
        GlobalStatic.Sound    = new AndroidPlatformSound();
        GlobalStatic.Lifecycle = new AndroidLifecycle(this);

        // 2. Create the engine console host and surface view
        //    Surface view is created first; console is wired after construction.
        var surfaceView = new GameSurfaceView(this);
        var host = new AndroidConsoleHost(surfaceView);
        _console = new EmueraConsole(host);
        surfaceView.SetConsole(_console);

        // 3. Build layout: console surface + input bar
        var rootLayout = new LinearLayout(this);
        rootLayout.Orientation = global::Android.Widget.Orientation.Vertical;

        var surfaceParams = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 0, 1f);
        rootLayout.AddView(surfaceView, surfaceParams);

        var inputBar = new InputBarView(this);
        inputBar.SetConsole(_console);
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
                _console.Initialize();
            }
            catch (Exception ex)
            {
                CrashLogger.LogException(ex, "Engine thread");
            }
        });
        _engineThread.IsBackground = true;
        _engineThread.Name = "EmueraEngine";
        _engineThread.Start();
    }

    protected override void OnDestroy()
    {
        _console?.Quit();
        GlobalStatic.Reset();
        base.OnDestroy();
    }
}
