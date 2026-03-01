namespace MinorShift.Emuera.Platform;

/// <summary>
/// Platform abstraction for file-system paths.
/// Replaces hard-coded Program.WorkingDir / Sys.WorkingDir paths.
/// </summary>
public interface IPlatformPaths
{
    /// <summary>Root directory of the currently loaded ERA game.</summary>
    string GameRootDirectory { get; }

    /// <summary>Directory used for save data.</summary>
    string SaveDirectory { get; }

    /// <summary>Full path to the emuera.config file.</summary>
    string ConfigFilePath { get; }
}
