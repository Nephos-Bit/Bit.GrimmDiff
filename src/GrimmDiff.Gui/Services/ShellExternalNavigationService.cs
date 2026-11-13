using GrimmDiff.Logic.Abstracts;
using System.Diagnostics;

namespace GrimmDiff.Gui;

public sealed class ShellExternalNavigationService : IExternalNavigationService
{
    public void Open(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}