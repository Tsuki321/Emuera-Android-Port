# Emuera Android Port

An Android port of [Emuera](http://osdn.jp/projects/emuera/) — the ERA-script interpreter originally written for Windows by MinorShift and 妊）|дﾟ)の中の人. This project lets you load and play ERA-format games on your Android phone.

---

## Table of Contents

- [What is ERA / Emuera?](#what-is-era--emuera)
- [Features](#features)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Requirements](#requirements)
- [Building from Source](#building-from-source)
- [Running on Device / Emulator](#running-on-device--emulator)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)
- [Credits](#credits)

---

## What is ERA / Emuera?

ERA is a scripting language and game format originating from Japanese eroge (adult visual novel) culture. Games written in ERA consist of `.ERB` script files and resource files (images, CSV data, sound). Emuera is an open-source interpreter for these games on Windows.

This project ports Emuera's engine to Android so players can enjoy ERA games on mobile devices without a PC.

---

## Features

- **ERA-script interpreter** — runs the same ERB scripts as the desktop Emuera
- **SkiaSharp rendering** — fast, hardware-accelerated console display
- **Input bar** — on-screen text input and quick digit buttons (0–9) for ERA `INPUT` commands
- **Game folder picker** — uses Android's Storage Access Framework to open any folder on-device
- **Audio** — background music and sound effects via Android `MediaPlayer`
- **Save/load** — saves stored in app-private internal storage (no extra permissions needed)
- **CI pipeline** — GitHub Actions builds the engine and APK on every push

---

## Architecture

The codebase is split into two projects:

```
┌────────────────────────────────────────────────────┐
│               Emuera.Engine  (Class Library)        │
│  GameProc / GameData / GameView (platform-agnostic) │
│  Interfaces: IConsoleHost, IPlatformDialogs,        │
│              IPlatformSound, IPlatformPaths,        │
│              IPlatformLifecycle                     │
└────────────────────────────────────────────────────┘
            │  project reference
┌───────────────────────────────────────────────────┐
│           Emuera.Android  (net9.0-android)         │
│  MainActivity  — game folder picker                │
│  GameActivity  — boots the engine                  │
│  GameSurfaceView — SkiaSharp ERA console renderer  │
│  InputBarView   — text + number input              │
│  AndroidConsoleHost — bridges engine ↔ Android UI  │
│  Platform/  — Android implementations of           │
│               IPlatformPaths, IPlatformDialogs,    │
│               IPlatformSound, IPlatformLifecycle   │
└───────────────────────────────────────────────────┘
```

The engine never references any Android or Windows SDK; all platform calls go through the interfaces registered in `GlobalStatic`.

---

## Project Structure

```
Emuera-Android-Port/
├── Emuera.Engine/          # Platform-agnostic ERA engine (C# class library)
│   ├── GameProc/           # ERB interpreter & execution loop
│   ├── GameData/           # Variable store & expression evaluator
│   ├── GameView/           # Console model (display lines, input state)
│   ├── Config/             # Engine configuration
│   ├── Content/            # Image & resource handling
│   ├── Platform/           # Abstraction interfaces
│   └── AssemblyInfo.cs     # InternalsVisibleTo for Android & test projects
├── Emuera.Android/         # Android front-end (net9.0-android)
│   ├── MainActivity.cs     # Game folder picker activity
│   ├── GameActivity.cs     # Game engine host activity
│   ├── Views/
│   │   ├── GameSurfaceView.cs   # SkiaSharp console renderer
│   │   └── InputBarView.cs      # Input bar (text + digit buttons)
│   └── Platform/           # Android implementations of engine interfaces
├── Emuera.Tests/           # xUnit unit tests for the engine
├── Emuera/                 # Original Windows Emuera source (reference)
├── Readme/                 # Original Emuera documentation & licenses
├── docs/                   # Development plans
├── LICENSE                 # zlib/libpng license
└── Emuera.sln
```

---

## Requirements

| Tool | Version |
|---|---|
| .NET SDK | 9.0 or later |
| Android workload | `dotnet workload install android` |
| Android API level | 21+ (Android 5.0 Lollipop) |

---

## Building from Source

### 1. Clone the repository

```bash
git clone https://github.com/Tsuki321/Emuera-Android-Port.git
cd Emuera-Android-Port
```

### 2. Install the Android workload

```bash
dotnet workload install android
```

### 3. Build the engine library

```bash
dotnet build Emuera.Engine/Emuera.Engine.csproj
```

### 4. Run the unit tests

```bash
dotnet test Emuera.Tests/Emuera.Tests.csproj
```

### 5. Build the Android APK

```bash
dotnet build Emuera.Android/Emuera.Android.csproj -c Release -p:AndroidKeyStore=false
```

The APK is output to `Emuera.Android/bin/Release/net9.0-android/`.

---

## Running on Device / Emulator

1. Enable **Developer Options** and **USB Debugging** on your device (or start an emulator).
2. Install the APK:
   ```bash
   adb install Emuera.Android/bin/Release/net9.0-android/com.yourname.emuera-Signed.apk
   ```
3. Launch **Emuera** on the device.
4. Tap **Pick Game Folder** and select the folder containing your ERA game's `.ERB` scripts.
5. The game will start automatically.

---

## Usage

| UI element | Purpose |
|---|---|
| **Pick Game Folder** button | Opens the Android folder picker to select an ERA game |
| **Game screen** | Scrollable ERA console — swipe up/down to scroll, tap buttons to activate them |
| **Input bar (bottom)** | Type a response and press **OK**, or tap a digit button (0–9) for numeric `INPUT` commands |

### Input types

- **INPUT** (numeric) — tap one of the digit buttons 0–9 or type a number and press OK
- **INPUTS** (text) — type into the text box and press OK or the keyboard **Done** action
- **Button tap** — any ERA `PRINT` button rendered in the game display can be tapped directly

---

## Contributing

Pull requests are welcome. Please open an issue first to discuss significant changes.

### Code style

- C# 12 / .NET 9
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Engine code must not reference `Android.*`, `System.Windows.Forms`, or `System.Drawing`

### Running CI locally

```bash
# Build engine + run tests
dotnet build Emuera.Engine/Emuera.Engine.csproj
dotnet test Emuera.Tests/Emuera.Tests.csproj

# Build APK (requires Android workload)
dotnet build Emuera.Android/Emuera.Android.csproj -c Release -p:AndroidKeyStore=false
```

---

## License

This project is licensed under the **zlib/libpng license** — see [LICENSE](LICENSE) for details.

The original Emuera engine is copyright © 2008– MinorShift and 妊）|дﾟ)の中の人, used under the same zlib/libpng license.

Third-party components:
- **libwebp** — Copyright © 2010 Google Inc., BSD 3-Clause license (see `Readme/License/LibWebp.LICENSE.txt`)

---

## Credits

| Name | Role |
|---|---|
| MinorShift, 妊）|дﾟ)の中の人 | Original Emuera 1.824 engine |
| evilmask | Emuera.EM — modified Emuera files used as the base for this port |
| Enter | EmueraEE extensions (sound, WebP, extra commands) |
| Tsuki321 | Android port |
