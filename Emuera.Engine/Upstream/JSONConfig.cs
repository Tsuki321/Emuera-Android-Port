//新設したコンフィグ設定のロード、セーブ、公開を担当する。
using System.IO;
using System.Text.Json;


namespace MinorShift.Emuera.JSON;
static class JSONConfig
{
	public static JSONConfigData Data = new();

	const string _configFileName = "setting.json";
	static string _configFilePath => Path.Combine(Path.GetDirectoryName(GlobalStatic.Paths?.ConfigFilePath) ?? Program.ExeDir, _configFileName);

	public static void Load()
	{
		if (!File.Exists(_configFilePath))
		{
			var defaultData = new JSONConfigData();
			var defaultJson = JsonSerializer.Serialize(defaultData);
			File.WriteAllText(_configFilePath, defaultJson);
		}

		var json = File.ReadAllText(_configFilePath);

		Data = JsonSerializer.Deserialize<JSONConfigData>(json);
	}

	public static void Save()
	{
		var json = JsonSerializer.Serialize(Data);
		File.WriteAllText(_configFilePath, json);
	}
}
