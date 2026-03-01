using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace MinorShift._Library;

public static class Sys
{
	static Sys()
	{
		ExePath = Assembly.GetEntryAssembly().Location; 
		#region eee_カレントディレクトリー
		WorkingDir = Directory.GetCurrentDirectory() + "\\";
		#endregion
		ExeDir = Path.GetDirectoryName(ExePath) + "\\";
		ExeName = Path.GetFileName(ExePath);
	}

	/// <summary>
	/// 実行ファイルのパス
	/// </summary>
	public static readonly string ExePath;

	/// <summary>
	/// 実行ファイルのディレクトリ。最後に\を付けたstring
	/// </summary>
	public static readonly string ExeDir;

	#region eee_カレントディレクトリー
	/// <summary>
	/// 実行ファイルのディレクトリ。最後に\を付けたstring
	/// </summary>
	public static readonly string WorkingDir;
	#endregion

	/// <summary>
	/// 実行ファイルの名前。ディレクトリなし
	/// </summary>
	public static readonly string ExeName;

	/// <summary>
	/// 2重起動防止。既に同名exeが実行されているならばtrueを返す
	/// </summary>
	/// <returns></returns>
	public static bool PrevInstance()
	{
		string thisProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
		if (System.Diagnostics.Process.GetProcessesByName(thisProcessName).Length > 1)
		{
			return true;
		}
		return false;

	}
}
