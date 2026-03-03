using MinorShift.Emuera.GameData;
using Xunit;

namespace Emuera.Tests;

public class ConstantDataTests
{
	[Fact]
	public void GetAliasPath_UnixLikePath_UsesSameDirectory()
	{
		string csvPath = "/tmp/game/csv/ABL.CSV";
		string aliasPath = ConstantData.GetAliasPath(csvPath);

		Assert.Equal("/tmp/game/csv/ABL.als", aliasPath);
	}

	[Fact]
	public void GetAliasPath_WindowsLikePath_ChangesOnlyExtension()
	{
		string csvPath = @"C:\game\csv\ABL.CSV";
		string aliasPath = ConstantData.GetAliasPath(csvPath);

		Assert.Equal(@"C:\game\csv\ABL.als", aliasPath);
	}
}
