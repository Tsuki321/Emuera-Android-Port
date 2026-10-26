This original, non-adult fixture exercises the real interpreter without downloading a game.

Open it from the app's **Compatibility check** button. Enter `42`, tap the console at WAIT,
wait for `TIMER_DONE`, enter some text, and tap **Choose OK**. The output should include
`ANSWER=42`, `WAIT_DONE`, `TIMER_DONE`, your text, and `BUTTON=OK`.

CI runs these scripts through the engine on Linux, additionally checking save/load.
Device checks should cover readable CJK text, keyboard opening, scrolling, tap targets,
Back/return/relaunch, and absence of crashes in logcat. This fixture does not establish
compatibility with all Emuera games or exercise every graphics, audio, or HTML command.
