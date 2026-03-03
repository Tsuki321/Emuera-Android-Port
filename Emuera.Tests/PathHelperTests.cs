using System.IO;
using MinorShift.Emuera.Sub;
using Xunit;

namespace Emuera.Tests;

/// <summary>Tests for <see cref="PathHelper"/> case-insensitive resolution helpers.</summary>
public class PathHelperTests
{
    // ── ResolveDirectoryCaseInsensitive ──────────────────────────────────

    [Fact]
    public void ResolveDirectory_ExistingPath_ReturnedUnchanged()
    {
        // Use the system temp directory which always exists.
        string existing = Path.GetTempPath().TrimEnd('/').TrimEnd(Path.DirectorySeparatorChar);
        string result = PathHelper.ResolveDirectoryCaseInsensitive(existing);
        // Whatever case the OS reports, the path must exist.
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void ResolveDirectory_NullOrEmpty_ReturnedAsIs()
    {
        Assert.Equal("", PathHelper.ResolveDirectoryCaseInsensitive(""));
        Assert.Null(PathHelper.ResolveDirectoryCaseInsensitive(null));
    }

    [Fact]
    public void GetFilesIgnoreCase_MissingDirectory_ReturnsEmpty()
    {
        // A directory that definitely does not exist should return an empty array,
        // not throw a DirectoryNotFoundException.
        string missing = Path.Combine(Path.GetTempPath(), "emuera_test_nonexistent_xyz");
        string[] result = PathHelper.GetFilesIgnoreCase(missing, "*.erb");
        Assert.Empty(result);
    }

    [Fact]
    public void GetFilesIgnoreCase_ExistingDirectory_FindsFiles()
    {
        string dir = Path.Combine(Path.GetTempPath(), "emuera_pathhelper_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "TestFile.ERB"), "");
            File.WriteAllText(Path.Combine(dir, "another.erb"), "");
            File.WriteAllText(Path.Combine(dir, "skip.txt"), "");

            string[] results = PathHelper.GetFilesIgnoreCase(dir, "*.ERB");
            Assert.Equal(2, results.Length);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void FindFileCaseInsensitive_MissingFile_ReturnsOriginal()
    {
        string missing = Path.Combine(Path.GetTempPath(), "emuera_no_such_file.csv");
        string result = PathHelper.FindFileCaseInsensitive(missing);
        Assert.Equal(missing, result);
    }

    [Fact]
    public void FindFileCaseInsensitive_ExistingFileWithDifferentCase_ResolvesRealFile()
    {
        string dir = Path.Combine(Path.GetTempPath(), "emuera_case_file_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            string realPath = Path.Combine(dir, "TestAlias.ALS");
            File.WriteAllText(realPath, "");

            string requestedPath = Path.Combine(dir, "testalias.als");
            string result = PathHelper.FindFileCaseInsensitive(requestedPath);

            Assert.True(File.Exists(result));
            Assert.Equal(realPath, result);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
