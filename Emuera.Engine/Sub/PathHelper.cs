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
	/// On case-sensitive file systems, resolves each component of
	/// <paramref name="path"/> case-insensitively so that a game whose
	/// directories are stored as "ERB/" or "Csv/" is found even though
	/// the engine requests "erb/" or "csv/".
	/// Returns the original path unchanged when the platform is case-insensitive
	/// or when no case-insensitive match exists.
	/// </summary>
	public static string ResolveDirectoryCaseInsensitive(string path)
	{
		if (string.IsNullOrEmpty(path) || !_isCaseSensitive)
			return path;
		if (Directory.Exists(path))
			return path;

		// Normalise separators and strip a trailing slash so GetFileName works.
		string normalised = path.Replace('\\', '/').TrimEnd('/');
		string parent = Path.GetDirectoryName(normalised);
		string segment = Path.GetFileName(normalised);

		if (string.IsNullOrEmpty(segment))
			return path; // root or drive letter — can't resolve further

		// Recursively resolve the parent first.
		string resolvedParent = string.IsNullOrEmpty(parent)
			? parent
			: ResolveDirectoryCaseInsensitive(parent);

		if (string.IsNullOrEmpty(resolvedParent) || !Directory.Exists(resolvedParent))
			return path;

		string match = Directory.GetDirectories(resolvedParent)
			.FirstOrDefault(d => string.Equals(
				Path.GetFileName(d), segment, StringComparison.OrdinalIgnoreCase));

		// Preserve a trailing separator if the original path had one.
		char sep = path[^1] is '/' or '\\' ? path[^1] : '\0';
		if (match != null)
			return sep != '\0' ? match + sep : match;

		// No match found — return with resolved parent so at least the root is correct.
		return sep != '\0'
			? Path.Combine(resolvedParent, segment) + sep
			: Path.Combine(resolvedParent, segment);
	}

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
		if (string.IsNullOrEmpty(name))
			return path;

		// Resolve the directory itself case-insensitively before looking for the file.
		string resolvedDir = string.IsNullOrEmpty(dir) ? dir : ResolveDirectoryCaseInsensitive(dir);
		if (string.IsNullOrEmpty(resolvedDir) || !Directory.Exists(resolvedDir))
			return path;

		string match = Directory.GetFiles(resolvedDir)
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

		// Resolve the directory itself case-insensitively before listing its contents.
		string resolvedPath = ResolveDirectoryCaseInsensitive(path);
		if (!Directory.Exists(resolvedPath))
			return [];

		// On a case-sensitive OS, enumerate all files and filter manually.
		string patternLower = searchPattern.ToLowerInvariant();
		return Directory.GetFiles(resolvedPath, "*", option)
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
