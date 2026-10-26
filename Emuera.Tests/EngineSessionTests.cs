using System.Drawing;
using System.Text;
using MinorShift.Emuera;
using MinorShift.Emuera.GameProc;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Platform;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Emuera.Tests;

public class EngineSessionTests
{
    [Fact]
    public async Task MixedCaseGameRunsInputAndSavesOnTheWorker()
    {
        string root = Path.Combine(Path.GetTempPath(), "emuera-session-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(root, "CSV"));
        Directory.CreateDirectory(Path.Combine(root, "ERB"));
        File.WriteAllText(Path.Combine(root, "CSV", "GAMEBASE.CSV"), "タイトル,Compatibility fixture\nコード,20261026\nバージョン,1000\n", Encoding.UTF8);
        File.WriteAllText(Path.Combine(root, "ERB", "MAIN.ERB"), """
            @SYSTEM_TITLE
            PRINTL READY 日本語 中文
            INPUT
            PRINTFORML ANSWER={RESULT}
            FLAG:0 = RESULT
            WAIT
            PRINTL WAIT_DONE
            TWAIT 100, 1
            PRINTL TIMER_DONE
            INPUTS
            PRINTFORML TEXT=%RESULTS%
            WAIT
            """, Encoding.UTF8);
        var host = new TestHost();
        using var dispatcher = new EngineDispatcher();
        Exception? error = null;
        dispatcher.UnhandledException += ex => error = ex;
        EmueraConsole? console = null;
        try
        {
            GlobalStatic.Reset();
            GlobalStatic.Paths = new TestPaths(root);
            GlobalStatic.Dialogs = new TestDialogs();
            await Run(dispatcher, () =>
            {
                ConfigData.Instance.LoadConfig();
                console = new EmueraConsole(host) { Dispatcher = dispatcher };
                console.Initialize();
            });
            Assert.Null(error);
            Assert.Contains("READY 日本語 中文", Output(console!));
            Assert.Equal(InputType.IntValue, console!.inputReq.InputType);
            await Run(dispatcher, () => console.PressEnterKey(false, "42", false));
            Assert.Contains("ANSWER=42", Output(console));
            await Run(dispatcher, () =>
            {
                Assert.True(GlobalStatic.VEvaluator.SaveTo(0, "fixture"));
                Assert.True(GlobalStatic.VEvaluator.LoadFrom(0));
            });
            Assert.NotEmpty(Directory.GetFiles(GlobalStatic.Paths.SaveDirectory));
            await Run(dispatcher, () => console.PressEnterKey(false, "", false));
            for (int i = 0; i < 100; i++)
            {
                bool done = false;
                await Run(dispatcher, () => done = Output(console).Contains("TIMER_DONE"));
                if (done) break;
                await Task.Delay(20);
            }
            await Run(dispatcher, () =>
            {
                Assert.Contains("TIMER_DONE", Output(console));
                Assert.Equal(InputType.StrValue, console.inputReq.InputType);
                console.PressEnterKey(false, "日本語 input", false);
                Assert.Contains("TEXT=日本語 input", Output(console));
            });
            Assert.Null(error);
        }
        finally
        {
            await Run(dispatcher, () => console?.Quit());
            dispatcher.Dispose();
            await dispatcher.Completion.WaitAsync(TimeSpan.FromSeconds(10));
            GlobalStatic.Reset();
            Directory.Delete(root, true);
        }
    }

    private static string Output(EmueraConsole console) => string.Join("\n", console.DisplayLineList.Select(x => x.ToString()));

    private static Task Run(EngineDispatcher dispatcher, Action action)
    {
        var result = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dispatcher.Post(() =>
        {
            try { action(); result.SetResult(); }
            catch (Exception ex) { result.SetException(ex); }
        });
        return result.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    private sealed class TestPaths(string root) : IPlatformPaths
    {
        public string GameRootDirectory => root;
        public string SaveDirectory => Path.Combine(root, "private-saves");
        public string ConfigFilePath => Path.Combine(root, "emuera.config");
        public string LogDirectory => root;
    }

    private sealed class TestDialogs : IPlatformDialogs
    {
        public Task<bool> ShowYesNoAsync(string message, string title) => Task.FromResult(false);
        public Task ShowInfoAsync(string message, string title) => Task.CompletedTask;
    }

    private sealed class TestHost : IConsoleHost
    {
        public void RequestRedraw() { }
        public void BeginWaitInput() { }
        public event Action<string>? InputSubmitted { add { } remove { } }
        public int ConsoleWidth => 800;
        public int ConsoleHeight => 600;
        public void ScrollToBottom() { ScrollPosition = MaxScrollPosition; }
        public bool IsAvailable => true;
        public int ScrollPosition { get; set; }
        public int MaxScrollPosition { get; set; }
        public void SetWindowTitle(string title) { }
        public string GetWindowTitle() => "Fixture";
        public string InputBoxText { get; set; } = "";
        public bool InputBoxPositionChanged => false;
        public void ResetInputBoxPosition() { }
        public void SetInputBoxBackColor(Color color) { }
        public void ShowConfigDialog() { }
        public void SetTooltip(string text) { }
        public Point? GetConsoleRelativeCursorPosition() => null;
    }
}
