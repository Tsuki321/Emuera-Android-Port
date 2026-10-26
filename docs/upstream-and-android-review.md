# Upstream and Android compatibility review — 2026-10-26

## Source lineage and update

The local checkout began at `b0af131` (initial port), 102 commits behind the Android
repository's `main`. It was fast-forwarded to `c7389d5` before further work. The original
Windows source identified its lineage as Emuera 1.824 → private modifications v21 →
Emuera.EM v18 → EmueraEE v47. The Android repository is not a GitHub fork, so its remote
alone did not identify the interpreter upstream.

The authoritative source is [EvilMask/emuera.em on GitLab](https://gitlab.com/EvilMask/emuera.em).
Its current `master`, checked on October 26, is
[`7b69ebd27378c03c32b6477b74901bfc3d33223c`](https://gitlab.com/EvilMask/emuera.em/-/commit/7b69ebd27378c03c32b6477b74901bfc3d33223c),
dated August 13, version **1.824+v24+EMv18+EEv56**. The last upstream change exposes
GameBase version metadata through CHKDATA. The previous base tag resolves to
`80b07e29f21889254c5de31eca23beebd4702b6d` (May 27, 2024).

Upstream reorganized the application into `Runtime/` and `UI/`, moved namespaces,
renamed loader/stream/expression types, changed expression arguments to lists, and
adopted .NET 10. This is not an ordinary merge between shared Git histories.

`Emuera/`, `EmueraPluginExample/`, and `Readme/` now contain the pinned upstream reference.
The portable runtime was merged against the EEv47 baseline, mapping upstream types into
the existing engine namespaces and preserving Android/SkiaSharp replacements. Parser,
variable, save, and instruction changes were integrated. **The Android display adapter
is not a complete port of EEv56's desktop UI.** The version pin identifies provenance,
not a promise of full feature parity.

Other public projects inspected during discovery included
[andEmuera](https://github.com/BluishCat/andEmuera) and
[EmueraEX](https://github.com/BluishCat/EmueraEX). They use a different platform design
and additional integration patches; they are related forks, not this port's upstream.
No code from those projects was imported.

## Architecture and findings

| Area | Finding and changes |
|---|---|
| Interpreter | `GameProc` executes ERB/ERH, `GameData` supplies variables and built-ins, `Sub` handles parsing and serialization. These consume process-wide `GlobalStatic` state. |
| Android execution | Startup already used a thread, but subsequent input executed on the Android UI thread. Input and timers now use one dispatcher. Session workers share a gate because globals prohibit concurrent games. Rendering attempts the gate without blocking the UI. |
| Timed input | Timer startup lived in a removed WinForms paint handler, and timers were one-shot. Timed waits now start when input begins, tick repeatedly, and coalesce queued ticks. Shutdown stops timers. |
| File compatibility | Case-sensitive Android paths require explicit Windows-style case matching. Fixed upstream enumeration of uppercase ERB directories, preloaded file reads, image lookup, and game-relative EXISTFILE/ENUMFILES/text files. Wildcards support `*` and `?`. |
| Encoding | A 4096-character sample misidentified Shift-JIS after a long ASCII header. Detection now validates the whole file with bounded memory, retaining BOM handling and stream position. |
| Saves | All games formerly shared one internal directory. Saves now use `files/games/<SHA-256 of canonical game path>/sav`. Identically named folders at different paths are isolated. Moving a game changes its storage identity. |
| Rendering | Android skipped non-text parts and sprite draw methods were empty. Static/animated sprites, inline images, rectangle shapes, background color, and basic HTML islands are now drawn. Animation uses a monotonic clock. |
| Button input | BINPUTS could accept `0` for string buttons and crashed because escaped display parts were initialized only by the removed desktop renderer. Both have regression coverage. |
| Lifecycle | Session reset clears cached scripts, content, configuration, fonts, console timers, and audio. Back/return/relaunch must still be checked on a device. |
| Build | Engine/tests/Android target .NET 10. GitHub Actions builds and tests on Linux and produces a Release APK; no local compilation was used. |

## Validation

The original non-adult fixture under `fixtures/compatibility` is shared by CI and the
Android **Compatibility check** button. It verifies mixed-case CSV/ERB startup, CJK text
through the interpreter, numeric/string input, WAIT/TWAIT, button input, and return flow.
The automated session additionally saves a value, changes it, reloads it, and checks the
stored GameBase version in both text and binary save formats. Pixel tests check static/animated sprite rendering; separate
tests cover encoding and game storage identities.

This validates actual script execution rather than only compiling the interpreter.
The suite currently contains 36 passing tests, with a successful Release APK build
([validation run](https://github.com/Tsuki321/Emuera-Android-Port/actions/runs/40334211674)).
The APK is `com.yourname.emuera`, version `1.1-preview` (code 2), target API 36, and
contains ARM64 and x86-64 native libraries. Device testing was deferred at the user's request. No real-game compatibility matrix
has yet been established, and text appearing in CI output does not prove Android glyph
layout is correct.

## Remaining compatibility work

- Storage uses a folder picker followed by a filesystem path conversion and broad file
  access. Arbitrary document providers/cloud folders are not supported. Replace this with
  an import or virtual filesystem design before claiming general SAF support.
- Full HTML/div positioning, borders, clipping, tooltips, CBG layers and button maps need
  a dedicated renderer. Basic HTML islands currently render from the top of the viewport.
- Some graphics color matrices, masks, and dash styles remain incomplete. Pixel tests do
  not cover the entire graphics instruction set.
- Text measurement and Android fallback rendering still need unified font selection and
  shaping. Check custom fonts, CJK line wrapping, and button hitboxes on the phone.
- Audio uses the existing MediaPlayer/SoundPool implementation. First-play SoundPool
  readiness, codec support, lifecycle pause/resume, and volume behavior need device tests.
- Desktop debugging, Rikaichan, Win32 hotkeys, and Windows native plugins are not Android
  equivalents. Hotkey-state functions report script errors on this host. Plugin loading
  remains experimental and Windows DLL dependencies cannot be assumed to work.
- Per-game saves deliberately do not guess ownership of the old shared `files/saves`
  directory. Those files are preserved; a user-facing import/export and migration tool
  remains needed. Do not uninstall the old app to resolve a signing mismatch without
  backing up saves first.
- .NET 10 builds successfully, but bundled native library compatibility (including 16 KB
  page-size devices), background suspension, large games, and app upgrades need ADB tests.
- Screen orientation remains portrait. No claim is made for every Emuera fork extension.

## Maintaining the upstream pin

`upstream.json` records exact source identity. To refresh only the desktop reference,
run `python scripts/update_upstream_reference.py <upstream-checkout>` after reviewing a
new pinned revision. This intentionally does not overwrite the Android engine. Review
runtime deltas separately against `Emuera/Runtime`, preserve the adapters, and add fixture
coverage for behavior changes. Build through the `Android Build` GitHub Actions workflow.

For the next phone session: install the downloaded signed APK with `adb install -r`,
launch Compatibility check, inspect logcat and app crash logs, then try several legally
available games covering UTF-8/Shift-JIS, graphics/HTML, audio, and large script sets.
