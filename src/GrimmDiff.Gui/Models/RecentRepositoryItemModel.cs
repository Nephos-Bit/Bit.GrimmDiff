using GrimmDiff.Logic.Abstracts;
namespace GrimmDiff.Gui;

public sealed record RecentRepositoryItemModel(string Path)
{
    public string Name => System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(Path));

    public override string ToString()
    {
        return Name;
    }
}