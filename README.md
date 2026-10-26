# Emuera Android Port

An Android port of [Emuera](http://osdn.jp/projects/emuera/) — the ERA-script interpreter originally written for Windows by MinorShift and 妊）|дﾟ)の中の人. This project lets you load and play ERA-format games on your Android phone.

---


## Current port status

The interpreter is based on [Emuera.EM+EE EEv56](https://gitlab.com/EvilMask/emuera.em/-/commit/7b69ebd27378c03c32b6477b74901bfc3d33223c)
(upstream checked October 26, 2026), with Android platform adaptations. The exact pin is
in [upstream.json](upstream.json). This remains a compatibility preview, not a claim that
every Emuera game or desktop extension works.

Use **Compatibility check** on the home screen to run a bundled non-adult fixture.
CI exercises actual ERB execution, input, timers, button validation, save/load, encoding,
and sprite rendering. Phone testing is still required for fonts, touch, audio, and lifecycle.
See the [codebase review and remaining work](docs/upstream-and-android-review.md).

Game saves are isolated in internal storage by game path. The old shared `files/saves`
directory is preserved but is not automatically assigned to a game; keep it backed up.

## Table of Contents

- [What is ERA / Emuera?](#what-is-era--emuera)
- [Features](#features)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Requirements](#requirements)
- [Building from Source](#building-from-source)
- [Running on Device / Emulator](#running-on-device--emulator)
- [Usage](#usage)
  - [Japanese / CJK Fonts](#japanese--cjk-fonts)
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
│           Emuera.Android  (net10.0-android)         │
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

The compiled engine uses platform interfaces and SkiaSharp. Excluded desktop library files remain as reference material.

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
├── Emuera.Android/         # Android front-end (net10.0-android)
│   ├── MainActivity.cs     # Game folder picker activity
│   ├── GameActivity.cs     # Game engine host activity
│   ├── Views/
│   │   ├── GameSurfaceView.cs   # SkiaSharp console renderer
│   │   └── InputBarView.cs      # Input bar (text + digit buttons)
│   └── Platform/           # Android implementations of engine interfaces
├── Emuera.Tests/           # xUnit unit tests for the engine
├── Emuera/                 # Pinned upstream Windows Emuera source (reference)
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

Builds and tests for this workspace run through **GitHub Actions**. Open the
[Android Build workflow](https://github.com/Tsuki321/Emuera-Android-Port/actions/workflows/android-build.yml),
choose **Run workflow**, and select the branch to validate. Pull requests and pushes to
`main`/`dev` also run CI. The workflow installs .NET 10 and the Android workload, builds the
engine, runs the test suite, and builds a Release APK.

Download `emuera-android` for the APK or `test-results` for TRX results. The unsigned and
development-signed APKs may both be present; install the `*-Signed.apk` file.

---

## Running on Device / Emulator

1. Enable **Developer Options** and **USB Debugging** on your device (or start an emulator).
2. Install the APK:
   ```bash
   adb install Emuera.Android/bin/Release/net10.0-android/com.yourname.emuera-Signed.apk
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

## Japanese / CJK Fonts

ERA games are almost always written in Japanese. Without a Japanese font, characters will render as empty boxes (tofu ▯). The app automatically loads any font files you place in the game's `font/` folder.

### Steps

1. **Download a free CJK font.** [Noto Sans JP](https://fonts.google.com/noto/specimen/Noto+Sans+JP) is recommended — it is open-source, covers hiragana, katakana, and common kanji, and is small enough for mobile use.
   - Visit the link above → click **Download family** → unzip the archive → take `NotoSansJP-Regular.ttf`.

2. **Create the `font/` folder** inside your ERA game's root directory — the same folder that contains `csv/`, `erb/`, etc.

   ```
   MyERAGame/
   ├── csv/
   ├── erb/
   ├── font/               ← create this folder
   │   └── NotoSansJP-Regular.ttf   ← place the font here
   └── emuera.config
   ```

3. **Launch the game.** Emuera Android scans `font/` at startup, detects which fonts can render Japanese glyphs, and uses them automatically. You should see Japanese text immediately.

> **Other font formats:** `.otf` and `.ttc` files are also supported. You can add multiple fonts; the app will pick the first one that contains the glyphs needed.

---

## Contributing

Pull requests are welcome. Please open an issue first to discuss significant changes.

### Code style

- C# 14 / .NET 10
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Engine code must not reference `Android.*`, `System.Windows.Forms`, or `System.Drawing`

### Validation

Run the **Android Build** GitHub Actions workflow on the branch under review. Tests are
in `Emuera.Tests`; the shared executable fixture is in `fixtures/compatibility`.
Do not treat a successful build alone as real-game compatibility validation.

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
