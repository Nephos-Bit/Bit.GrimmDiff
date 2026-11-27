using System.IO;
using System.Timers;
using System.Windows;
using GrimmDiff.Gui.Abstracts.Services;
using Timer = System.Timers.Timer;

namespace GrimmDiff.Gui.Services;

internal sealed class RepositoryRefreshScheduler : IDisposable
{
    private readonly IRepositoryChangeMonitorService _monitor;
    private readonly Action _onScheduled;
    private readonly Func<Task> _refresh;
    private readonly Timer _timer = new(500) { AutoReset = false };
    private int _running, _pending;

    public RepositoryRefreshScheduler(IRepositoryChangeMonitorService monitor, Action onScheduled, Func<Task> refresh)
    {
        _monitor = monitor;
        _onScheduled = onScheduled;
        _refresh = refresh;
        _timer.Elapsed += OnElapsed;
    }

    public void Watch(string path) => _monitor.Watch(path, Schedule);

    private void Schedule(string path)
    {
        if (IsGeneratedPath(path)) return;
        _onScheduled();
        Interlocked.Exchange(ref _pending, 1);
        _timer.Stop();
        _timer.Start();
    }

    private void OnElapsed(object? sender, ElapsedEventArgs e) =>
        _ = Application.Current.Dispatcher.InvokeAsync(ProcessQueueAsync);

    private async Task ProcessQueueAsync()
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0) return;
        try
        {
            do
            {
                Interlocked.Exchange(ref _pending, 0);
                await _refresh();
            } while (Interlocked.Exchange(ref _pending, 0) != 0);
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }

    private static bool IsGeneratedPath(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (segments.Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                                    segment.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                                    segment.Equals("node_modules", StringComparison.OrdinalIgnoreCase) ||
                                    segment.Equals(".vs", StringComparison.OrdinalIgnoreCase))) return true;
        var gitIndex = Array.FindIndex(segments, segment => segment.Equals(".git", StringComparison.OrdinalIgnoreCase));
        if (gitIndex < 0 || gitIndex + 1 >= segments.Length) return gitIndex >= 0;
        var gitEntry = segments[gitIndex + 1];
        return !gitEntry.Equals("HEAD", StringComparison.OrdinalIgnoreCase)
               && !gitEntry.Equals("refs", StringComparison.OrdinalIgnoreCase)
               && !gitEntry.Equals("packed-refs", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _timer.Dispose();
        _monitor.Dispose();
    }
}
