namespace GrimmDiff.App;

public sealed record RecentRepositoryItem(string Path)
{
    public string Name => System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(Path));
    public override string ToString() => Name;
}
