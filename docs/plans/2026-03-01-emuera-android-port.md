# Emuera Android Port — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Port the Emuera ERA-script interpreter to Android so players can load and run ERA-format games on their phones.

**Architecture:** Split the existing codebase into a platform-agnostic engine library (C# class library, reusing `GameProc`, `GameData`, `GameView` logic) and a new Android-specific front-end project (Activity + SkiaSharp canvas). WinForms and GDI+ are replaced behind thin abstraction interfaces so the engine never calls platform code directly.

**Tech Stack:** .NET 9 for Android, SkiaSharp (rendering), Android MediaPlayer/SoundPool (audio), Android Storage Access Framework (file picking), AndroidX ViewModel/LiveData (state), GitHub Actions for CI.

---

## Architecture Overview

```
┌────────────────────────────────────────────────────┐
│                 Emuera.Engine (Class Library)       │
│  GameProc / GameData / GameView (logic only)        │
│  Depends on: IConsoleHost, IPlatform, IDialogHost   │
└────────────────────────────────────────────────────┘
        │ (project reference)
┌───────────────────────────────────────────────────┐
│              Emuera.Android (net9.0-android)       │
│  MainActivity → GameActivity → GameSurfaceView     │
│  AndroidConsoleHost (implements IConsoleHost)       │
│  AndroidPlatform   (implements IPlatform)           │
│  SkiaSharp canvas rendering                        │
│  ExoPlayer / MediaPlayer audio                     │
└───────────────────────────────────────────────────┘
```

### Key Existing Files (Engine — Keep/Adapt)
| Path | Role |
|---|---|
| `GameProc/Process.cs` + partials | ERB interpreter, execution loop |
| `GameProc/ErbLoader.cs / HeaderFileLoader.cs` | Script parsing |
| `GameProc/LogicalLineParser.cs` | Line-level parser |
| `GameData/Variable/VariableData.cs` | Variable store |
| `GameData/Expression/ExpressionMediator.cs` | Expression evaluator |
| `GameView/EmueraConsole.cs` + partials | Console model (print buffer, input state) |
| `GameView/ConsoleDisplayLine.cs` etc. | Render data model |
| `Config/Config.cs` | Settings |

### Files to Replace / Stub on Android
| Path | Replacement |
|---|---|
| `Forms/MainWindow.cs` | `GameActivity.kt` + `GameSurfaceView` |
| `GameView/EmueraConsole.cs` (WinForms parts) | `AndroidConsoleHost` (implements new `IConsoleHost`) |
| `_Library/WinInput.cs` | Android touch / keyboard events |
| `_Library/WinmmTimer.cs` | `android.os.Handler.postDelayed` |
| `_Library/Sound.WMP.cs` / `Sound.NAudio.cs` | `AndroidSoundPlayer` (MediaPlayer) |
| `_Library/Sys.cs` | `AndroidSys` (Android context paths) |
| `_Library/WebPWrapper.cs` | Android `BitmapFactory` (native WebP support) |
| `System.Drawing.*` GDI+ calls | SkiaSharp `SKBitmap`, `SKCanvas`, `SKPaint` |
| `System.Windows.Forms.MessageBox` | Android `AlertDialog` |
| `System.Windows.Forms.Timer` | Android Handler |

---

## Phase 1 — Project Setup & Solution Structure ✅ COMPLETE (2026-03-01)

### Task 1: Create the Engine Class Library project ✅

**Files:**
- Create: `Emuera.Engine/Emuera.Engine.csproj`

**Step 1: Create the csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <RootNamespace>MinorShift.Emuera</RootNamespace>
  </PropertyGroup>
</Project>
```

**Step 2: Add it to solution**

In `Emuera.sln` add the new project. From the repo root:
```
dotnet sln add Emuera.Engine/Emuera.Engine.csproj
```

**Step 3: Copy (do not cut yet) source files into `Emuera.Engine/`**

Copy these folders wholesale:
- `Emuera/GameProc/` → `Emuera.Engine/GameProc/`
- `Emuera/GameData/` → `Emuera.Engine/GameData/`
- `Emuera/GameView/` → `Emuera.Engine/GameView/`
- `Emuera/Config/` → `Emuera.Engine/Config/`
- `Emuera/Content/` → `Emuera.Engine/Content/`
- `Emuera/Sub/` → `Emuera.Engine/Sub/`
- `Emuera/GlobalStatic.cs` → `Emuera.Engine/GlobalStatic.cs`

**Step 4: Verify the engine project compiles (errors expected — fix in later tasks)**

```
dotnet build Emuera.Engine/Emuera.Engine.csproj
```

Expected: Many errors around `System.Windows.Forms` and `System.Drawing`.

---

### Task 2: Create the Android project ✅

**Files:**
- Create: `Emuera.Android/Emuera.Android.csproj`

**Step 1: Create a new .NET for Android application project**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-android</TargetFramework>
    <OutputType>Exe</OutputType>
    <ApplicationId>com.yourname.emuera</ApplicationId>
    <ApplicationVersion>1</ApplicationVersion>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Emuera.Engine\Emuera.Engine.csproj" />
    <PackageReference Include="SkiaSharp.Views.Android" Version="2.*" />
    <PackageReference Include="SkiaSharp" Version="2.*" />
  </ItemGroup>
</Project>
```

**Step 2: Create minimal `MainActivity.cs` and `AndroidManifest.xml`**

`Emuera.Android/MainActivity.cs`:
```csharp
using Android.App;
using Android.OS;

namespace Emuera.Android;

[Activity(Label = "Emuera", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);
    }
}
```

**Step 3: Add to the solution**
```
dotnet sln add Emuera.Android/Emuera.Android.csproj
```

---

### Task 3: Create the GitHub Actions CI workflow ✅

**Files:**
- Create: `.github/workflows/android-build.yml`

```yaml
name: Android Build

on:
  push:
    branches: [ main, dev ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Install Android workload
        run: dotnet workload install android

      - name: Build Engine
        run: dotnet build Emuera.Engine/Emuera.Engine.csproj --no-restore

      - name: Build Android APK
        run: dotnet build Emuera.Android/Emuera.Android.csproj -c Release
```

---

## Phase 2 — Abstract Platform Dependencies (Engine Side) ✅ COMPLETE

The goal of this phase is to make `Emuera.Engine` compile with `net9.0` (no platform deps). We introduce interfaces so the engine can call the Android host without referencing `System.Windows.Forms`.

### Task 4: Define core platform abstraction interfaces

**Files:**
- Create: `Emuera.Engine/Platform/IConsoleHost.cs`
- Create: `Emuera.Engine/Platform/IPlatformDialogs.cs`
- Create: `Emuera.Engine/Platform/IPlatformTimer.cs`
- Create: `Emuera.Engine/Platform/IPlatformSound.cs`
- Create: `Emuera.Engine/Platform/IPlatformPaths.cs`

**`IConsoleHost.cs`** — the main rendering/input contract (replaces `MainWindow` + `EmueraConsole`'s WinForms surface):
```csharp
namespace MinorShift.Emuera.Platform;

public interface IConsoleHost
{
    // Called by Process to signal the view to redraw
    void RequestRedraw();
    // Called when the engine enters input-wait state
    void BeginWaitInput(InputRequestData request);
    // Called to deliver the result of user input back to engine
    event Action<string> InputSubmitted;
    // Screen dimensions used for layout
    int ConsoleWidth { get; }
    int ConsoleHeight { get; }
    // Scroll management
    void ScrollToBottom();
}
```

**`IPlatformDialogs.cs`**:
```csharp
namespace MinorShift.Emuera.Platform;

public interface IPlatformDialogs
{
    // Replaces System.Windows.Forms.MessageBox.Show(...)
    Task<bool> ShowYesNoAsync(string message, string title);
    Task ShowInfoAsync(string message, string title);
}
```

**`IPlatformTimer.cs`**:
```csharp
namespace MinorShift.Emuera.Platform;

public interface IPlatformTimer : IDisposable
{
    void Start(int intervalMs, Action callback);
    void Stop();
}
```

**`IPlatformSound.cs`**:
```csharp
namespace MinorShift.Emuera.Platform;

public interface IPlatformSound : IDisposable
{
    void PlayBGM(string filePath, bool loop);
    void PlaySE(string filePath);
    void StopBGM();
    void StopAll();
    void SetBGMVolume(int volume);  // 0–100
}
```

**`IPlatformPaths.cs`**:
```csharp
namespace MinorShift.Emuera.Platform;

public interface IPlatformPaths
{
    string GameRootDirectory { get; }
    string SaveDirectory { get; }
    string ConfigFilePath { get; }
}
```

---

### Task 5: Replace `System.Windows.Forms` usage in `Config/Config.cs`

**Files:**
- Modify: `Emuera.Engine/Config/Config.cs`

**Step 1: Remove `using System.Windows.Forms;`**

**Step 2: Replace every `MessageBox.Show(...)` call with a call through `IPlatformDialogs`**

A static holder in `GlobalStatic` will hold the registered dialogs instance:
```csharp
// In GlobalStatic.cs add:
public static IPlatformDialogs Dialogs;
```

Replace patterns like:
```csharp
// OLD
MessageBox.Show(trmb.ConfigFileError.Text, trmb.ConfigError.Text, MessageBoxButtons.YesNo)
// NEW — note: this call site must become async or use a sync shim
GlobalStatic.Dialogs.ShowYesNoAsync(trmb.ConfigFileError.Text, trmb.ConfigError.Text).GetAwaiter().GetResult()
```

**Step 3: Replace `System.Drawing.Color` and `System.Drawing.Font` references**

Add a compatibility shim or switch to `SkiaSharp.SKColor` / `SKTypeface` for these config values. The safest first step is to introduce `EngineColor` and `EngineFont` value structs in `Emuera.Engine/Platform/`:

```csharp
// Emuera.Engine/Platform/EngineTypes.cs
namespace MinorShift.Emuera.Platform;

public record struct EngineColor(byte R, byte G, byte B, byte A = 255);
public record struct EngineFont(string FamilyName, float SizeInPoints, bool Bold, bool Italic);
```

Swap all `System.Drawing.Color` usages in `Config.cs` to `EngineColor`. Add conversion extensions as needed.

---

### Task 6: Replace `System.Windows.Forms` usage in `Process.cs` and `Process.*.cs`

**Files:**
- Modify: `Emuera.Engine/GameProc/Process.cs`
- Modify: `Emuera.Engine/GameProc/Process.SystemProc.cs`
- Modify: `Emuera.Engine/GameProc/Process.ScriptProc.cs`

**Step 1: Remove all `using System.Windows.Forms;` statements**

**Step 2: Replace `MessageBox.Show(...)` with `GlobalStatic.Dialogs.ShowYesNoAsync(...).GetAwaiter().GetResult()`**

Search for every call site:
```
grep -rn "MessageBox.Show" Emuera.Engine/
```

**Step 3: Replace `Application.Exit()` calls**

Add `IPlatformLifecycle.RequestExit()` to `GlobalStatic` and call it instead:
```csharp
// Emuera.Engine/Platform/IPlatformLifecycle.cs
public interface IPlatformLifecycle
{
    void RequestExit();
    void RequestRestart();
}
```

---

### Task 7: Replace `System.Drawing` GDI+ in `GameView/` and `Content/`

**Files:**
- Modify: `Emuera.Engine/GameView/StringMeasure.cs`
- Modify: `Emuera.Engine/Content/ImgUtils.cs`
- Modify: `Emuera.Engine/Content/GraphicsImage.cs`
- Modify: `Emuera.Engine/Content/ConstImage.cs`
- Modify: `Emuera.Engine/Content/CroppedImage.cs`

**Step 1: Add SkiaSharp nuget reference to Engine project**

```xml
<PackageReference Include="SkiaSharp" Version="2.*" />
```

**Step 2: Create `IImageHandle` to wrap rendered images**

```csharp
// Emuera.Engine/Platform/IImageHandle.cs
namespace MinorShift.Emuera.Platform;

public interface IImageHandle : IDisposable
{
    int Width { get; }
    int Height { get; }
}
```

**Step 3: Replace `System.Drawing.Bitmap` in `GraphicsImage`, `ConstImage`, `CroppedImage` with `IImageHandle`**

The concrete `SKBitmap`-backed implementation lives in `Emuera.Android/AndroidImageHandle.cs`.

**Step 4: Replace `System.Drawing.Graphics` text measure in `StringMeasure.cs`**

`StringMeasure` currently uses `Graphics.MeasureString`. Replace with SkiaSharp:
```csharp
// Before (GDI+)
SizeF size = g.MeasureString(text, font);

// After (SkiaSharp)
var paint = new SKPaint { Typeface = ..., TextSize = ... };
float width = paint.MeasureText(text);
```

**Step 5: Build check**
```
dotnet build Emuera.Engine/Emuera.Engine.csproj
```
Target: zero `System.Windows.Forms` and zero `System.Drawing` compile errors.

---

### Task 8: Replace `_Library/` Windows stubs in Engine

**Files:**
- Modify: `Emuera.Engine/_Library/Sys.cs`
- Delete from Engine: `Emuera.Engine/_Library/WinInput.cs`
- Delete from Engine: `Emuera.Engine/_Library/WinmmTimer.cs`
- Delete from Engine: `Emuera.Engine/_Library/Sound.WMP.cs`
- Delete from Engine: `Emuera.Engine/_Library/Sound.NAudio.cs`

**Step 1: Replace `Sys.cs` with interface-backed version**

Remove the `Assembly.GetEntryAssembly()` path resolution. Instead read paths from `GlobalStatic.Paths` (`IPlatformPaths`):
```csharp
public static string WorkingDir => GlobalStatic.Paths.GameRootDirectory;
```

**Step 2: Replace Sound calls**

Search all call sites of `Sound.PlayBGM(...)`, `Sound.PlaySE(...)` etc. in engine code and redirect to `GlobalStatic.Sound` (`IPlatformSound`).

**Step 3: Remove `WinInput.cs` and `WinmmTimer.cs` from the Engine project file**

These are Windows-only. The Android project will provide its own input handling and timer via `IPlatformTimer`.

---

## Phase 3 — Android Front-End Implementation ✅ COMPLETE (2026-03-01)

### Task 9: Implement `AndroidPlatformPaths`

**Files:**
- Create: `Emuera.Android/Platform/AndroidPlatformPaths.cs`

```csharp
using Android.Content;
using MinorShift.Emuera.Platform;

namespace Emuera.Android.Platform;

public class AndroidPlatformPaths(Context context, string gameRootUri) : IPlatformPaths
{
    public string GameRootDirectory { get; } = gameRootUri;
    public string SaveDirectory { get; } = 
        Path.Combine(context.FilesDir!.AbsolutePath, "saves");
    public string ConfigFilePath { get; } = 
        Path.Combine(context.FilesDir!.AbsolutePath, "emuera.config");
}
```

---

### Task 10: Implement `AndroidPlatformDialogs`

**Files:**
- Create: `Emuera.Android/Platform/AndroidPlatformDialogs.cs`

```csharp
using Android.App;
using MinorShift.Emuera.Platform;

namespace Emuera.Android.Platform;

public class AndroidPlatformDialogs(Activity activity) : IPlatformDialogs
{
    public Task<bool> ShowYesNoAsync(string message, string title)
    {
        var tcs = new TaskCompletionSource<bool>();
        activity.RunOnUiThread(() =>
        {
            new AlertDialog.Builder(activity)!
                .SetTitle(title)!
                .SetMessage(message)!
                .SetPositiveButton("Yes", (_, _) => tcs.SetResult(true))!
                .SetNegativeButton("No",  (_, _) => tcs.SetResult(false))!
                .Show();
        });
        return tcs.Task;
    }

    public Task ShowInfoAsync(string message, string title)
    {
        var tcs = new TaskCompletionSource();
        activity.RunOnUiThread(() =>
        {
            new AlertDialog.Builder(activity)!
                .SetTitle(title)!
                .SetMessage(message)!
                .SetPositiveButton("OK", (_, _) => tcs.SetResult())!
                .Show();
        });
        return tcs.Task;
    }
}
```

---

### Task 11: Implement `AndroidPlatformSound`

**Files:**
- Create: `Emuera.Android/Platform/AndroidPlatformSound.cs`

```csharp
using Android.Media;
using MinorShift.Emuera.Platform;

namespace Emuera.Android.Platform;

public class AndroidPlatformSound : IPlatformSound
{
    private MediaPlayer? _bgmPlayer;

    public void PlayBGM(string filePath, bool loop)
    {
        _bgmPlayer?.Stop();
        _bgmPlayer?.Dispose();
        _bgmPlayer = new MediaPlayer();
        _bgmPlayer.SetDataSource(filePath);
        _bgmPlayer.Looping = loop;
        _bgmPlayer.Prepare();
        _bgmPlayer.Start();
    }

    public void StopBGM()
    {
        _bgmPlayer?.Stop();
    }

    public void PlaySE(string filePath)
    {
        // Use SoundPool for short sound effects
        // (SoundPool setup omitted for brevity — load on demand)
    }

    public void StopAll()
    {
        StopBGM();
    }

    public void SetBGMVolume(int volume)
    {
        float v = volume / 100f;
        _bgmPlayer?.SetVolume(v, v);
    }

    public void Dispose()
    {
        _bgmPlayer?.Dispose();
    }
}
```

---

### Task 12: Implement the Android console renderer (`GameSurfaceView`)

The `EmueraConsole` in the engine builds a list of `ConsoleDisplayLine` objects (styled text, images, buttons). The Android front-end reads this list and renders it via SkiaSharp.

**Files:**
- Create: `Emuera.Android/Views/GameSurfaceView.cs`

Architecture:
- Extends `SKCanvasView` from SkiaSharp.Views.Android
- Holds a reference to `EmueraConsole` (engine-side model)
- On `OnPaintSurface`: iterate `console.DisplayLines`, draw each part with `SKCanvas`
- Touch events are translated to button clicks / input submission

```csharp
using Android.Content;
using Android.Views;
using SkiaSharp;
using SkiaSharp.Views.Android;
using MinorShift.Emuera.GameView;

namespace Emuera.Android.Views;

public class GameSurfaceView(Context context, EmueraConsole console) : SKCanvasView(context)
{
    private readonly EmueraConsole _console = console;

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Black);
        DrawConsole(canvas, e.Info.Width, e.Info.Height);
    }

    private void DrawConsole(SKCanvas canvas, int width, int height)
    {
        // TODO: iterate _console.DisplayLines, for each ConsoleDisplayLine
        // render ConsoleStyledString parts as SKPaint text draws
        // render ConsoleImagePart as DrawBitmap
        // render ConsoleButtonString with tap-region tracking
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        // TODO: Hit-test touch point against button regions
        // If hit: call _console.ReceiveInput(buttonValue)
        return base.OnTouchEvent(e);
    }
}
```

**Implementation detail for text rendering:**
```csharp
var paint = new SKPaint
{
    Color = SKColor.Parse(part.Color.ToString()),
    TextSize = part.FontSize,
    Typeface = SKTypeface.FromFamilyName(part.FontName),
    IsAntialias = true,
};
canvas.DrawText(part.Text, x, y, paint);
```

---

### Task 13: Implement `AndroidConsoleHost` (bridge between engine and view)

**Files:**
- Create: `Emuera.Android/Platform/AndroidConsoleHost.cs`

```csharp
using MinorShift.Emuera.Platform;
using MinorShift.Emuera.GameView;
using Emuera.Android.Views;

namespace Emuera.Android.Platform;

public class AndroidConsoleHost(GameSurfaceView surfaceView) : IConsoleHost
{
    private readonly GameSurfaceView _view = surfaceView;

    public int ConsoleWidth => _view.Width;
    public int ConsoleHeight => _view.Height;

    public event Action<string>? InputSubmitted;

    public void RequestRedraw()
    {
        _view.PostInvalidate(); // thread-safe invalidate
    }

    public void BeginWaitInput(InputRequestData request)
    {
        // Show soft keyboard or input UI panel
        // When user confirms input, fire InputSubmitted
    }

    public void ScrollToBottom()
    {
        // Notify view to scroll to end of display list
    }

    // Called from Android UI when user submits text/number
    public void SubmitInput(string value)
    {
        InputSubmitted?.Invoke(value);
    }
}
```

---

### Task 14: Wire up `GameActivity` — the main game screen

**Files:**
- Create: `Emuera.Android/GameActivity.cs`

```csharp
using Android.App;
using Android.OS;
using MinorShift.Emuera;
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.GameView;
using Emuera.Android.Platform;
using Emuera.Android.Views;

namespace Emuera.Android;

[Activity(Label = "Game", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
public class GameActivity : Activity
{
    private Thread? _engineThread;
    private EmueraConsole? _console;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        string gameRoot = Intent!.GetStringExtra("GAME_ROOT")!;

        // 1. Register platform services in GlobalStatic
        GlobalStatic.Paths    = new AndroidPlatformPaths(this, gameRoot);
        GlobalStatic.Dialogs  = new AndroidPlatformDialogs(this);
        GlobalStatic.Sound    = new AndroidPlatformSound();

        // 2. Create engine console + Android view
        _console = new EmueraConsole();          // now decoupled from WinForms
        var surfaceView = new GameSurfaceView(this, _console);
        var host = new AndroidConsoleHost(surfaceView);
        _console.SetHost(host);

        SetContentView(surfaceView);

        // 3. Boot engine on background thread
        _engineThread = new Thread(() =>
        {
            var process = new Process(_console, false);
            GlobalStatic.Process = process;
            process.Initialize();
            process.Run();
        });
        _engineThread.IsBackground = true;
        _engineThread.Start();
    }

    protected override void OnDestroy()
    {
        GlobalStatic.Reset();
        base.OnDestroy();
    }
}
```

---

### Task 15: Game-selection / file-picker `MainActivity`

The player must pick a game folder from device storage (Android SAF).

**Files:**
- Create: `Emuera.Android/MainActivity.cs`
- Create: `Emuera.Android/Resources/layout/activity_main.xml`

**Step 1: activity_main.xml**
```xml
<?xml version="1.0" encoding="utf-8"?>
<LinearLayout xmlns:android="http://schemas.android.com/apk/res/android"
    android:layout_width="match_parent"
    android:layout_height="match_parent"
    android:orientation="vertical"
    android:gravity="center">

    <Button android:id="@+id/btn_pick_game"
        android:layout_width="wrap_content"
        android:layout_height="wrap_content"
        android:text="Pick Game Folder" />

    <ListView android:id="@+id/list_recent_games"
        android:layout_width="match_parent"
        android:layout_height="0dp"
        android:layout_weight="1" />
</LinearLayout>
```

**Step 2: MainActivity.cs — launch SAF folder picker**
```csharp
[Activity(Label = "Emuera", MainLauncher = true)]
public class MainActivity : Activity
{
    private const int RequestPickFolder = 1001;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);
        FindViewById<Button>(Resource.Id.btn_pick_game)!.Click += (_, _) => PickFolder();
    }

    private void PickFolder()
    {
        var intent = new Intent(Intent.ActionOpenDocumentTree);
        StartActivityForResult(intent, RequestPickFolder);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode == RequestPickFolder && resultCode == Result.Ok && data?.Data != null)
        {
            string path = ResolveUriToPath(data.Data);
            var gameIntent = new Intent(this, typeof(GameActivity));
            gameIntent.PutExtra("GAME_ROOT", path);
            StartActivity(gameIntent);
        }
    }

    private string ResolveUriToPath(Android.Net.Uri uri)
    {
        // Use DocumentFile or StorageManager to get real path
        // Store URI permission for future access
        ContentResolver!.TakePersistableUriPermission(uri,
            ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
        return uri.Path!; // simplified — real implementation uses DocumentFile API
    }
}
```

---

## Phase 4 — Input System

### Task 16: Touch-based input handling

ERA games use numeric input (press a number to select an option) and text input. The input bar replaces the WinForms `richTextBox1` input field.

**Files:**
- Modify: `Emuera.Android/GameActivity.cs`
- Create: `Emuera.Android/Views/InputBarView.cs`

**Step 1: Add input bar layout below the surface view**

Add a horizontal `LinearLayout` at the bottom:
- `EditText` for typed input (shown during `INPUTS` commands)
- `Button` "Enter" to submit
- Programmatic number buttons (1-9) shown during `INPUT` commands

**Step 2: Wire events**
```csharp
submitButton.Click += (_, _) =>
{
    string val = inputEditText.Text ?? "";
    _host.SubmitInput(val);
    inputEditText.Text = "";
};
```

**Step 3: Keyboard handling for physical keyboard (optional)**

Listen for `KeyEvent.ActionDown` with `Keycode.Enter`.

---

### Task 17: Scroll handling

ERA console is a scrollable log. Display lines accumulate and the player scrolls up.

**Files:**
- Modify: `Emuera.Android/Views/GameSurfaceView.cs`

**Step 1: Track scroll offset in `GameSurfaceView`**
```csharp
private float _scrollOffsetY = 0f;
private float _totalContentHeight = 0f;

// In OnTouchEvent, handle ACTION_MOVE to scroll
case MotionEventActions.Move:
    float dy = e.GetY() - _lastTouchY;
    _scrollOffsetY = Math.Clamp(_scrollOffsetY - dy, 0, 
        Math.Max(0, _totalContentHeight - Height));
    Invalidate();
    break;
```

**Step 2: Auto-scroll to bottom when new lines are added (driven by `IConsoleHost.ScrollToBottom`)**

---

## Phase 5 — Save / Load System

### Task 18: Adapt save/load to Android storage

The engine uses `File.ReadAllBytes/WriteAllBytes` with paths from `IPlatformPaths.SaveDirectory`. Since `IPlatformPaths` returns a files-dir path (internal storage), no special permissions are needed.

**Files:**
- Modify: `Emuera.Engine/GameProc/Process.SystemProc.cs` (save/load sections)

**Step 1: Find all hard-coded path constructions in save/load functions**

```
grep -n "ExeDir\|WorkingDir\|SaveDir" Emuera.Engine/GameProc/Process.SystemProc.cs
```

**Step 2: Replace with `GlobalStatic.Paths.SaveDirectory`**

```csharp
// Before
string savePath = Program.WorkingDir + "sav\\";
// After
string savePath = GlobalStatic.Paths.SaveDirectory + Path.DirectorySeparatorChar;
```

**Step 3: Ensure save directory exists**
```csharp
Directory.CreateDirectory(GlobalStatic.Paths.SaveDirectory);
```

---

## Phase 6 — Polish & CI Validation

### Task 19: Add engine unit tests project

**Files:**
- Create: `Emuera.Tests/Emuera.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <ProjectReference Include="..\Emuera.Engine\Emuera.Engine.csproj" />
  </ItemGroup>
</Project>
```

**Step 1: Write smoke tests for the expression evaluator**
```csharp
public class ExpressionTests
{
    [Fact]
    public void IntLiteral_EvaluatesToInt()
    {
        // Setup minimal GlobalStatic mocks ...
        var result = ExpressionMediator.Evaluate("1 + 2");
        Assert.Equal(3L, result);
    }
}
```

**Step 2: Add tests to CI workflow**
```yaml
- name: Run unit tests
  run: dotnet test Emuera.Tests/Emuera.Tests.csproj --logger trx
```

---

### Task 20: Update CI to build and sign APK in release

**Files:**
- Modify: `.github/workflows/android-build.yml`

```yaml
- name: Build Release APK
  run: |
    dotnet build Emuera.Android/Emuera.Android.csproj \
      -c Release \
      -p:AndroidKeyStore=false

- name: Upload APK artifact
  uses: actions/upload-artifact@v4
  with:
    name: emuera-android
    path: Emuera.Android/bin/Release/net9.0-android/*.apk
```

---

## Implementation Order Summary

| Phase | Tasks | Goal |
|---|---|---|
| 1 ✅ | 1–3 | Solution scaffolding + CI skeleton |
| 2 | 4–8 | Engine compiles for `net9.0` (no WinForms) |
| 3 | 9–15 | Android host runs the engine, renders output |
| 4 | 16–17 | Input & scrolling usable |
| 5 | 18 | Save/load works on-device |
| 6 | 19–20 | Tests + signed APK via CI |

---

## Known Risks & Mitigations

| Risk | Mitigation |
|---|---|
| `EmueraConsole` is 2692 lines tightly coupled to WinForms timers/events | Introduce `IConsoleHost` first; migrate call by call per compile error |
| Android scoped storage (API 30+) blocks direct file path access | Use SAF `DocumentFile` API; expose byte-stream read in `IPlatformPaths` |
| ERA scripts use Shift-JIS encoding | `System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` — already in `Program.cs`; carry forward to Android startup |
| `System.Drawing.Font` metrics differ from SkiaSharp | Column-width calculations (used for text layout) must be re-validated with SkiaSharp `SKPaint.MeasureText` |
| WinForms STA threading model vs Android Looper | Engine runs on a dedicated background thread; all Android UI calls must use `Activity.RunOnUiThread` or `Handler.Post` |
| Plugin system (`GameProc/PluginSystem/`) uses `Assembly.LoadFrom` | Plugins are unsupported on Android (no dynamic loading); stub or disable `PluginManager` with `#if !ANDROID` |
