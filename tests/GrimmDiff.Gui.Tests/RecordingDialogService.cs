using GrimmDiff.Gui.Abstracts.Services;

namespace GrimmDiff.Gui.Tests;

internal sealed class RecordingDialogService : IUserDialogService
{
    public Queue<string?> Answers { get; } = new();
    public int Questions { get; private set; }
    public string? SelectRepository() => null;
    public string? Ask(string title, string prompt)
    {
        Questions++;
        return Answers.Dequeue();
    }
    public bool Confirm(string title, string message) => false;
    public void ShowInformation(string title, string message) { }
    public void ShowReleaseNotes(string title, string notes) { }
}