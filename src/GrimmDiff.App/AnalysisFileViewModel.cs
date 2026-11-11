using GrimmDiff.Domain;

namespace GrimmDiff.App;

public sealed class AnalysisFileViewModel(string path, IReadOnlyList<DiffHunk> hunks, string groupTitle, string groupDescription)
{
    public string Path { get; } = path;
    public IReadOnlyList<DiffHunk> Hunks { get; } = hunks;
    public string GroupTitle { get; } = groupTitle;
    public string GroupDescription { get; } = groupDescription;
    public DiffRenderDocument? RenderDocument { get; internal set; }
    public override string ToString() => Path;
}
