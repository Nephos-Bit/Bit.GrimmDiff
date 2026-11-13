namespace GrimmDiff.Gui.Abstracts;

public interface IRepositoryChangeMonitorService : IDisposable
{
    void Watch(string path, Action<string> onChanged);
}