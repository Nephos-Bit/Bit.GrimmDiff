using System.IO;
namespace GrimmDiff.App;
public sealed class FileSystemRepositoryChangeMonitor : IRepositoryChangeMonitor
{
    private FileSystemWatcher? _watcher;
    private string? _path;
    private Action<string>? _onChanged;
    public void Watch(string path, Action<string> onChanged) { _watcher?.Dispose(); _path = path; _onChanged = onChanged; StartWatcher(); }
    private void StartWatcher()
    {
        if (_path is null || _onChanged is null) return;
        _watcher = new FileSystemWatcher(_path) { IncludeSubdirectories = true, EnableRaisingEvents = true, InternalBufferSize = 64 * 1024, NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite };
        _watcher.Changed += Notify; _watcher.Created += Notify; _watcher.Deleted += Notify; _watcher.Renamed += (_, e) => _onChanged?.Invoke(e.FullPath);
        _watcher.Error += (_, _) => { _watcher?.Dispose(); StartWatcher(); _onChanged?.Invoke(_path!); };
    }
    private void Notify(object? sender, FileSystemEventArgs e) => _onChanged?.Invoke(e.FullPath);
    public void Dispose() => _watcher?.Dispose();
}
