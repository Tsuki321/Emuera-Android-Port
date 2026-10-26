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
        foreach (string file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Fixtures"), "*", SearchOption.AllDirectories))
        {
            string destination = Path.Combine(root, Path.GetRelativePath(Path.Combine(AppContext.BaseDirectory, "Fixtures"), file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
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
            Assert.True(Output(console!).Contains("READY 日本語 中文"), Output(console!));
            Assert.Equal(InputType.IntValue, console!.inputReq.InputType);
            await Run(dispatcher, () => console.PressEnterKey(false, "42", false));
            Assert.Contains("ANSWER=42", Output(console));
            await Run(dispatcher, () =>
            {
                Assert.True(GlobalStatic.VEvaluator.SaveTo(0, "fixture"));
                var flag = GlobalStatic.VariableData.GetSystemVariableToken("FLAG");
                flag.SetValue(999L, [0L]);
                Assert.True(GlobalStatic.VEvaluator.LoadFrom(0));
                Assert.Equal(42, flag.GetIntValue(GlobalStatic.EMediator, [0L]));
                var data = GlobalStatic.VEvaluator.CheckData(0, MinorShift.Emuera.Sub.EraSaveFileType.Normal);
                Assert.Equal(1000, data.Version);
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
                Assert.Equal(InputType.StrButton, console.inputReq.InputType);
                long request = console.inputReq.ID;
                console.PressEnterKey(false, "0", false);
                Assert.Equal(request, console.inputReq.ID);
                console.PressEnterKey(false, "OK", true);
                Assert.Contains("BUTTON=OK", Output(console));
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
