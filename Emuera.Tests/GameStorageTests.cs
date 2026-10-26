using MinorShift.Emuera.Platform;
using Xunit;

namespace Emuera.Tests;

public class GameStorageTests
{
    [Fact]
    public void SameFolderNameInDifferentLocationsHasSeparateSaves()
    {
        Assert.NotEqual(GameStorage.GetGameId(Path.Combine(Path.GetTempPath(), "one", "game")),
            GameStorage.GetGameId(Path.Combine(Path.GetTempPath(), "two", "game")));
    }

    [Fact]
    public void TrailingSeparatorAndRelativeComponentsDoNotChangeIdentity()
    {
        string path = Path.Combine(Path.GetTempPath(), "game");
        Assert.Equal(GameStorage.GetGameId(path), GameStorage.GetGameId(path + Path.DirectorySeparatorChar));
        Assert.Equal(GameStorage.GetGameId(path), GameStorage.GetGameId(Path.Combine(path, ".")));
    }
}
