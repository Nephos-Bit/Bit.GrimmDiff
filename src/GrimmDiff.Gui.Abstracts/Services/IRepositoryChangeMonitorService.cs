namespace GrimmDiff.Gui.Abstracts.Services;

public interface IRepositoryChangeMonitorService : IDisposable
{
    void Watch(string path, Action<string> onChanged);
}