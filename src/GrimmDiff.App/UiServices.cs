using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace GrimmDiff.App;

public interface IUserDialogService
{
    string? SelectRepository();
    string? Ask(string title, string prompt);
}

public sealed class WpfUserDialogService : IUserDialogService
{
    public string? SelectRepository()
    {
        var dialog = new OpenFolderDialog { Title = "Open Git repository" };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
    public string? Ask(string title, string prompt) => InputDialog.Ask(title, prompt);
}

public interface IExternalNavigationService { void Open(string url); }
public sealed class ShellExternalNavigationService : IExternalNavigationService
{
    public void Open(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}

public interface IRepositoryChangeMonitor : IDisposable { void Watch(string path, Action<string> onChanged); }
public sealed class FileSystemRepositoryChangeMonitor : IRepositoryChangeMonitor
{
    private FileSystemWatcher? _watcher;
    public void Watch(string path, Action<string> onChanged)
    {
        _watcher?.Dispose();
        _watcher = new FileSystemWatcher(path) { IncludeSubdirectories = true, EnableRaisingEvents = true };
        _watcher.Changed += (_, e) => onChanged(e.FullPath);
        _watcher.Created += (_, e) => onChanged(e.FullPath);
        _watcher.Deleted += (_, e) => onChanged(e.FullPath);
        _watcher.Renamed += (_, e) => onChanged(e.FullPath);
    }
    public void Dispose() => _watcher?.Dispose();
}
