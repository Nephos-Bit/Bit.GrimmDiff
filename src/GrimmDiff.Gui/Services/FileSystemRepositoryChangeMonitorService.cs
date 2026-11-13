using GrimmDiff.Logic.Abstracts;
using System.IO;

namespace GrimmDiff.Gui;

public sealed class FileSystemRepositoryChangeMonitorService : IRepositoryChangeMonitorService
{
    private Action<string>? _onChanged;
    private string? _path;
    private FileSystemWatcher? _watcher;

    public void Watch(string path, Action<string> onChanged)
    {
        _watcher?.Dispose();
        _path = path;
        _onChanged = onChanged;
        StartWatcher();
    }

    public void Dispose()
    {
        _watcher?.Dispose();
    }

    #region Private

    private void StartWatcher()
    {
        if (_path is null || _onChanged is null) return;
        _watcher = new FileSystemWatcher(_path)
        {
            IncludeSubdirectories = true, EnableRaisingEvents = true, InternalBufferSize = 64 * 1024,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
        };
        _watcher.Changed += Notify;
        _watcher.Created += Notify;
        _watcher.Deleted += Notify;
        _watcher.Renamed += (_, e) => _onChanged?.Invoke(e.FullPath);
        _watcher.Error += (_, _) =>
        {
            _watcher?.Dispose();
            StartWatcher();
            _onChanged?.Invoke(_path!);
        };
    }

    private void Notify(object? sender, FileSystemEventArgs e)
    {
        _onChanged?.Invoke(e.FullPath);
    }

    #endregion
}