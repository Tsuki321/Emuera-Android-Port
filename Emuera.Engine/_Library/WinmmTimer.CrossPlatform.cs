using System;
using System.Diagnostics;

namespace MinorShift._Library;

/// <summary>
/// Cross-platform replacement for WinmmTimer (the original uses winmm.dll P/Invoke).
/// Uses System.Diagnostics.Stopwatch for high-resolution timing.
/// </summary>
internal static class WinmmTimer
{
	private static readonly Stopwatch _sw = Stopwatch.StartNew();

	/// <summary>
	/// Milliseconds elapsed since startup. Equivalent to timeGetTime().
	/// </summary>
	public static uint TickCount => (uint)(_sw.ElapsedMilliseconds & 0xFFFF_FFFF);

	/// <summary>
	/// The tick count value frozen at the start of the current frame.
	/// </summary>
	public static uint CurrentFrameTime { get; private set; }

	/// <summary>
	/// Call at the start of each render frame to snapshot the current time.
	/// </summary>
	public static void FrameStart() => CurrentFrameTime = TickCount;
}
