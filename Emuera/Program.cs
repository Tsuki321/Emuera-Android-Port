using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using MinorShift._Library;
using System.IO;
using EvilMask.Emuera;
using MinorShift.Emuera.GameProc.Function;
using MinorShift.Emuera.GameProc.PluginSystem;

namespace MinorShift.Emuera;

static class Program
{
	/*
	コードの開始地点。
	ここでMainWindowを作り、
	MainWindowがProcessを作り、
	ProcessがGameBase・ConstantData・Variableを作る。


	*.ERBの読み込み、実行、その他の処理をProcessが、
	入出力をMainWindowが、
	定数の保存をConstantDataが、
	変数の管理をVariableが行う。

	と言う予定だったが改変するうちに境界が曖昧になってしまった。

	後にEmueraConsoleを追加し、それに入出力を担当させることに。

	1750 DebugConsole追加
	 Debugを全て切り離すことはできないので一部EmueraConsoleにも担当させる

	TODO: 1819 MainWindow & Consoleの入力・表示組とProcess&Dataのデータ処理組だけでも分離したい

	*/
	/// <summary>
	/// アプリケーションのメイン エントリ ポイントです。
	/// </summary>
	[STAThread]
	static void Main(string[] args)
	{
		// memo: Shift-JISを扱うためのおまじない
		System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);


		// ExeDir = Sys.ExeDir;
		#region eee_カレントディレクトリー
		WorkingDir = Sys.WorkingDir;
#if DEBUG
		//debugMode = true;

		//ExeDirにバリアントのパスを代入することでテスト実行するためのコード。
		//ローカルパスの末尾には\必須。
		//ローカルパスを記載した場合は頒布前に削除すること。
		// ExeDir = @"";

#endif
		//CsvDir = ExeDir + "csv\\";
		//ErbDir = ExeDir + "erb\\";
		//DebugDir = ExeDir + "debug\\";
		//DatDir = ExeDir + "dat\\";
		//ContentDir = ExeDir + "resources\\";
		CsvDir = WorkingDir + "csv\\";
		ErbDir = WorkingDir + "erb\\";
		DebugDir = WorkingDir + "debug\\";
		DatDir = WorkingDir + "dat\\";
		ContentDir = WorkingDir + "resources\\";
		#region EE_フォントファイル対応
		FontDir = WorkingDir + "font\\";
		#endregion
		#endregion
		//エラー出力用
		//1815 .exeが東方板のNGワードに引っかかるそうなので除去
		ExeName = Path.GetFileNameWithoutExtension(Sys.ExeName);

		//WMPも終了しておく
		FunctionIdentifier.bgm.close();
		for (int i = 0; i < FunctionIdentifier.sound.Length; i++)
		{
			if (FunctionIdentifier.sound[i] != null) FunctionIdentifier.sound[i].close();
		}

		//解析モードの判定だけ先に行う
		//int argsStart = 0;
		#region EM_私家版_Emuera多言語化改造
		List<string> otherArgs = new List<string>();
		foreach (var arg in args)
		{
			//if ((args.Length > 0) && (args[0].Equals("-DEBUG", StringComparison.CurrentCultureIgnoreCase)))
			if (arg.Equals("-DEBUG", StringComparison.CurrentCultureIgnoreCase))
			{
				// argsStart = 1;//デバッグモードかつ解析モード時に最初の1っこ(-DEBUG)を飛ばす
				DebugMode = true;
			}
			else if (arg.Equals("-GENLANG", StringComparison.CurrentCultureIgnoreCase))
			{
				Lang.GenerateDefaultLangFile();
			}
			else otherArgs.Add(arg);
		}
		//if (args.Length > argsStart)
		if (otherArgs.Count > 0)
		{
			//必要なファイルのチェックにはConfig読み込みが必須なので、ここではフラグだけ立てておく
			AnalysisMode = true;
		}
		#endregion

		ApplicationConfiguration.Initialize();
		Application.SetCompatibleTextRenderingDefault(false);
		ConfigData.Instance.LoadConfig();

		#region EM_私家版_Emuera多言語化改造
		Lang.LoadLanguageFile();
		#endregion
		#region EM_私家版_Icon指定機能
		Icon icon = null;
		{
			var bmp = Utils.LoadImage(Utils.GetValidPath(Config.EmueraIcon));
			if (bmp != null)
			{
				icon = Utils.MakeIconFromBmpFile(bmp);
				bmp.Dispose();
			}
		}
		#endregion

		//二重起動の禁止かつ二重起動
		if ((!Config.AllowMultipleInstances) && Sys.PrevInstance())
		{
			//MessageBox.Show("多重起動を許可する場合、emuera.configを書き換えて下さい", "既に起動しています");
			MessageBox.Show(Lang.UI.MainWindow.MsgBox.MultiInstanceInfo.Text, Lang.UI.MainWindow.MsgBox.InstaceExists.Text);
			return;
		}
		if (!Directory.Exists(CsvDir))
		{
			//MessageBox.Show("csvフォルダが見つかりません", "フォルダなし");
			MessageBox.Show(Lang.UI.MainWindow.MsgBox.NoCsvFolder.Text, Lang.UI.MainWindow.MsgBox.FolderNotFound.Text);
			return;
		}
		if (!Directory.Exists(ErbDir))
		{
			//MessageBox.Show("erbフォルダが見つかりません", "フォルダなし");
			MessageBox.Show(Lang.UI.MainWindow.MsgBox.NoErbFolder.Text, Lang.UI.MainWindow.MsgBox.FolderNotFound.Text);
			return;
		}
		#region EE_フォントファイル対応
		//フォントファイルを読み込む
		if (Directory.Exists(FontDir))
		{
			foreach (string fontFile in Directory.GetFiles(Program.FontDir, "*.ttf", SearchOption.AllDirectories))
				GlobalStatic.Pfc.AddFontFile(fontFile);

			foreach (string fontFile in Directory.GetFiles(Program.FontDir, "*.otf", SearchOption.AllDirectories))
				GlobalStatic.Pfc.AddFontFile(fontFile);
		}
		#endregion

		if (DebugMode)
		{
			ConfigData.Instance.LoadDebugConfig();
			if (!Directory.Exists(DebugDir))
			{
				try
				{
					Directory.CreateDirectory(DebugDir);
				}
				catch
				{
					MessageBox.Show(Lang.UI.MainWindow.MsgBox.FailedCreateDebugFolder.Text, Lang.UI.MainWindow.MsgBox.FolderNotFound.Text);
					return;
				}
			}
		}
		if (AnalysisMode)
		{
			AnalysisFiles = new List<string>();
			#region EM_私家版_Emuera多言語化改造
			// for (int i = argsStart; i < args.Length; i++)
			foreach (var arg in otherArgs)
			{
				//if (!File.Exists(args[i]) && !Directory.Exists(args[i]))
				if (!File.Exists(arg) && !Directory.Exists(arg))
				{
					MessageBox.Show(Lang.UI.MainWindow.MsgBox.ArgPathNotExists.Text);
					return;
				}
				//if ((File.GetAttributes(args[i]) & FileAttributes.Directory) == FileAttributes.Directory)
				if ((File.GetAttributes(arg) & FileAttributes.Directory) == FileAttributes.Directory)
				{
					//List<KeyValuePair<string, string>> fnames = Config.GetFiles(args[i] + "\\", "*.ERB");
					List<KeyValuePair<string, string>> fnames = Config.GetFiles(arg + "\\", "*.ERB");
					for (int j = 0; j < fnames.Count; j++)
					{
						AnalysisFiles.Add(fnames[j].Value);
					}
				}
				else
				{
					//if (Path.GetExtension(args[i]).ToUpper() != ".ERB")
					if (Path.GetExtension(arg).ToUpper() != ".ERB")
					{
						MessageBox.Show(Lang.UI.MainWindow.MsgBox.InvalidArg.Text);
						return;
					}
					//AnalysisFiles.Add(args[i]);
					AnalysisFiles.Add(arg);
				}
			}
			#endregion
		}
		while (true)
		{
			var winState = FormWindowState.Normal;
			var rebootClientHeight = 0;
			var rebootLocation = Point.Empty;
			using var win = new MainWindow(winState, rebootLocation, rebootClientHeight, (_) =>
			{
				rebootFlag = true;
			});
			#region EM_私家版_Emuera多言語化改造
			win.TranslateUI();
			#endregion
			#region EM_私家版_Icon指定機能
			if (icon != null)
				win.SetupIcon(icon);
			#endregion
			Application.Run(win);
			Content.AppContents.UnloadContents();
			if (!rebootFlag)
				break;
			RebootWinState = win.WindowState;
			if (win.WindowState == FormWindowState.Normal)
			{
				rebootClientHeight = win.ClientSize.Height;
				rebootLocation = win.Location;
			}
			else
			{
				rebootClientHeight = 0;
				rebootLocation = new Point();
			}

			/* VVII版マージ前の起動処理
			MainWindow win = null;
			StartTime = WinmmTimer.TickCount;
			using (win = new MainWindow())
			{
				#region EM_私家版_Emuera多言語化改造
				win.TranslateUI();
				#endregion
				#region EM_私家版_Icon指定機能
				if (icon != null)
					win.SetupIcon(icon);
				#endregion
				Application.Run(win);
				Content.AppContents.UnloadContents();
				if (!Reboot)
					break;
				RebootWinState = win.WindowState;
				if (win.WindowState == FormWindowState.Normal)
				{
					RebootClientY = win.ClientSize.Height;
					RebootLocation = win.Location;
				}
				else
				{
					RebootClientY = 0;
					RebootLocation = new Point();
				}
			}
			*/
			//条件次第ではParserMediatorが空でない状態で再起動になる場合がある
			ParserMediator.ClearWarningList();
			ParserMediator.Initialize(null);
			GlobalStatic.Reset();
			//GC.Collect();
			#region EE_メモリリークの解決
			ConfigData.Instance.ReLoadConfig();
			break;
		}
		if (rebootFlag)
			Application.Restart();
		#endregion
	}

	#region eee_カレントディレクトリー
	/// <summary>
	/// 実行ファイルのディレクトリ。最後に\を付けたstring
	/// </summary>
	// public static string ExeDir { get; private set; }
	public static string WorkingDir { get; private set; }
	#endregion
	public static string CsvDir { get; private set; }
	public static string ErbDir { get; private set; }
	public static string DebugDir { get; private set; }
	public static string DatDir { get; private set; }
	public static string ContentDir { get; private set; }
	public static string ExeName { get; private set; }
	#region EE_PLAYSOUND系
	public static string MusicDir { get; private set; }
	#endregion
	#region EE_フォントファイル対応
	public static string FontDir { get; private set; }
	#endregion


	public static bool rebootFlag = false;
	//public static int RebootClientX = 0;
	//public static int RebootClientY = 0;
	public static FormWindowState RebootWinState = FormWindowState.Normal;
	//public static Point RebootLocation;

	public static bool AnalysisMode = false;
	public static List<string> AnalysisFiles = null;

	//public static bool debugMode = false;
	//public static bool DebugMode { get { return debugMode; } }
	public static bool DebugMode { get; private set; }

}
