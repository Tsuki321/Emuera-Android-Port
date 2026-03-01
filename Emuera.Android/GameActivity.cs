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
[Activity(Label = "Game", ScreenOrientation = global::Android.Content.PM.ScreenOrientation.Portrait)]
public class GameActivity : Activity
{
    private Thread? _engineThread;
    private EmueraConsole? _console;
    private AndroidConsoleHost? _host;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

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
        _host = new AndroidConsoleHost(surfaceView);
        _console = new EmueraConsole(_host);
        surfaceView.SetConsole(_console);

        // 3. Build layout: console surface + input bar
        var rootLayout = new LinearLayout(this);
        rootLayout.Orientation = global::Android.Widget.Orientation.Vertical;

        var surfaceParams = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, 0, 1f);
        rootLayout.AddView(surfaceView, surfaceParams);

        var inputBar = BuildInputBar(this, _host, _console);
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
                System.Diagnostics.Debug.WriteLine($"[Emuera] Engine exception: {ex}");
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

    /// <summary>Builds the bottom input bar with an EditText and submit button.</summary>
    private static View BuildInputBar(Activity activity, AndroidConsoleHost host, EmueraConsole console)
    {
        var layout = new LinearLayout(activity)
        {
            Orientation = global::Android.Widget.Orientation.Horizontal,
        };

        var editText = new EditText(activity)
        {
            Hint = "Input…",
            InputType = global::Android.Text.InputTypes.ClassText,
        };
        var editParams = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f);
        layout.AddView(editText, editParams);

        var submitBtn = new Button(activity) { Text = "OK" };
        layout.AddView(submitBtn);

        submitBtn.Click += (_, _) =>
        {
            string val = editText.Text ?? "";
            editText.Text = "";
            console.PressEnterKey(false, val, false);
        };

        return layout;
    }
}
