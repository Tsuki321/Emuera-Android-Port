namespace MinorShift.Emuera.GameView;

internal sealed partial class EmueraConsole
{
    public MinorShift.Emuera.Platform.EngineDispatcher Dispatcher { get; set; }
    public void Dispatch(Action action)
    {
        if (quitRequested) return;
        if (Dispatcher != null) Dispatcher.Post(action);
        else action();
    }
    public IReadOnlyList<ConsoleDisplayLine> HtmlIslandLines => htmlIslandLines;
    private readonly List<ConsoleDisplayLine> htmlIslandLines = new();

    public void PrintHTMLIsland(string html)
    {
        htmlIslandLines.AddRange(HtmlManager.Html2DisplayLine(html, stringMeasure, this));
        window?.RequestRedraw();
    }

    public void ClearHTMLIsland()
    {
        htmlIslandLines.Clear();
        window?.RequestRedraw();
    }

    public void PrintPlainWithSingleLineFix(string str)
    {
        var line = PrintPlainwithSingleLine(str);
        if (line == null) return;
        addDisplayLine(line, false);
        RefreshStrings(false);
    }
}
