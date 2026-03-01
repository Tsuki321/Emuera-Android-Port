using MinorShift.Emuera.Platform;

namespace MinorShift.Emuera;

/// <summary>
/// Engine-side compatibility shim that replaces the WinForms Program.cs.
/// Path properties are backed by GlobalStatic.Paths so they are available
/// after the host registers the IPlatformPaths implementation.
/// Flags (AnalysisMode, DebugMode) are set early by the host before calling Process.Initialize().
/// </summary>
internal static class Program
{
    // ── Paths (all delegate to IPlatformPaths) ────────────────────────────
    public static string WorkingDir
        => (GlobalStatic.Paths?.GameRootDirectory ?? "") + "/";

    public static string CsvDir
        => WorkingDir + "csv/";

    public static string ErbDir
        => WorkingDir + "erb/";

    public static string DebugDir
        => WorkingDir + "debug/";

    public static string DatDir
        => WorkingDir + "dat/";

    public static string ContentDir
        => WorkingDir + "resources/";

    public static string FontDir
        => WorkingDir + "font/";

    public static string MusicDir
        => WorkingDir + "bgm/";

    // ── State flags ───────────────────────────────────────────────────────
    /// <summary>Set by the host before Initialize() to run in analysis (syntax-check) mode.</summary>
    public static bool AnalysisMode { get; set; } = false;

    /// <summary>Set by the host before Initialize() to enable debug commands and views.</summary>
    public static bool DebugMode { get; set; } = false;

    /// <summary>Set to true to signal a restart is requested.</summary>
    public static bool rebootFlag { get; set; } = false;

    /// <summary>Files to analyse in analysis mode (set by host).</summary>
    public static List<string> AnalysisFiles { get; set; } = null;

    // ── Executable identity (informational only) ──────────────────────────
    public static string ExeName { get; set; } = "Emuera";
}
