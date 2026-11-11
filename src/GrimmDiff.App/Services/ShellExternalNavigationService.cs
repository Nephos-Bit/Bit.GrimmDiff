using System.Diagnostics;
namespace GrimmDiff.App;
public sealed class ShellExternalNavigationService : IExternalNavigationService { public void Open(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
