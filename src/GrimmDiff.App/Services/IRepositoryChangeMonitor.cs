namespace GrimmDiff.App;
public interface IRepositoryChangeMonitor : IDisposable { void Watch(string path, Action<string> onChanged); }
