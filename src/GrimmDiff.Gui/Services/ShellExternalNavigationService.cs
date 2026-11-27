using System.Diagnostics;
using GrimmDiff.Gui.Abstracts.Services;

namespace GrimmDiff.Gui.Services;

public sealed class ShellExternalNavigationService : IExternalNavigationService
{
    public void Open(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}