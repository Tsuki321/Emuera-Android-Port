using System.Collections.Concurrent;

namespace MinorShift.Emuera.Platform;

/// <summary>Serializes interpreter callbacks away from the UI thread.</summary>
public sealed class EngineDispatcher : IDisposable
{
    // The legacy interpreter uses process-wide globals. Session workers must
    // never run concurrently, including while a previous Activity is closing.
    public static object SessionSyncRoot { get; } = new();
    private readonly BlockingCollection<Action> queue = new();
    private int disposed;
    public object SyncRoot => SessionSyncRoot;
    public event Action<Exception> UnhandledException;
    private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Completion => completion.Task;

    public EngineDispatcher()
    {
        new Thread(Run) { IsBackground = true, Name = "Emuera interpreter" }.Start();
    }

    public void Post(Action action)
    {
        if (Volatile.Read(ref disposed) != 0) return;
        try { queue.Add(action); }
        catch (InvalidOperationException) { }
    }

    private void Run()
    {
        try
        {
            foreach (var action in queue.GetConsumingEnumerable())
            {
                if (Volatile.Read(ref disposed) != 0) break;
                try { lock (SyncRoot) action(); }
                catch (Exception ex) { UnhandledException?.Invoke(ex); }
            }
        }
        finally { completion.TrySetResult(); }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0) queue.CompleteAdding();
    }
}
