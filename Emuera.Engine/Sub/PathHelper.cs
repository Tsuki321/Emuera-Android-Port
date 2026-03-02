using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MinorShift.Emuera.Sub;

/// <summary>
/// File-system helpers that work correctly on both Windows (case-insensitive)
/// and Android/Linux (case-sensitive).
/// </summary>
internal static class PathHelper
{
	// Cache the platform check so we don't call RuntimeInformation on every access.
	private static readonly bool _isCaseSensitive =
		RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
		RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);

	/// <summary>
	/// Returns the real file path on disk, using a case-insensitive lookup when
	/// the file is not found at <paramref name="path"/> and the filesystem is
	/// case-sensitive (Linux / Android).
	/// Falls back to the original <paramref name="path"/> when no match is found.
	/// </summary>
	public static string FindFileCaseInsensitive(string path)
	{
		if (string.IsNullOrEmpty(path))
			return path;
		if (File.Exists(path))
			return path;
		if (!_isCaseSensitive)
			return path; // Windows: trust the original path

		string dir = Path.GetDirectoryName(path);
		string name = Path.GetFileName(path);
		if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name) || !Directory.Exists(dir))
			return path;

		string match = Directory.GetFiles(dir)
			.FirstOrDefault(f => string.Equals(
				Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase));
		return match ?? path;
	}

	/// <summary>
	/// Equivalent to <see cref="Directory.GetFiles(string, string, SearchOption)"/>
	/// but with case-insensitive pattern matching on case-sensitive file systems
	/// (Linux / Android).
	/// Supports simple glob patterns: <c>*.ext</c> and <c>prefix*.ext</c>.
	/// </summary>
	public static string[] GetFilesIgnoreCase(string path, string searchPattern,
		SearchOption option = SearchOption.TopDirectoryOnly)
	{
		if (!_isCaseSensitive)
			return Directory.GetFiles(path, searchPattern, option);

		// On a case-sensitive OS, enumerate all files and filter manually.
		string patternLower = searchPattern.ToLowerInvariant();
		return Directory.GetFiles(path, "*", option)
			.Where(f => MatchesGlob(Path.GetFileName(f).ToLowerInvariant(), patternLower))
			.ToArray();
	}

	/// <summary>
	/// Minimal glob match that handles a single <c>*</c> wildcard.
	/// All patterns used by the engine are of the form <c>*.ext</c> or
	/// <c>prefix*.ext</c> — patterns with multiple wildcards are not supported.
	/// Both <paramref name="fileNameLower"/> and <paramref name="patternLower"/>
	/// must already be lower-cased by the caller.
	/// </summary>
	private static bool MatchesGlob(string fileNameLower, string patternLower)
	{
		int starIdx = patternLower.IndexOf('*');
		if (starIdx < 0)
			return fileNameLower == patternLower;

		ReadOnlySpan<char> prefix = patternLower.AsSpan(0, starIdx);
		ReadOnlySpan<char> suffix = patternLower.AsSpan(starIdx + 1);
		ReadOnlySpan<char> name   = fileNameLower.AsSpan();
		return name.StartsWith(prefix, StringComparison.Ordinal)
			&& name.EndsWith(suffix, StringComparison.Ordinal)
			&& name.Length >= prefix.Length + suffix.Length;
	}
}
