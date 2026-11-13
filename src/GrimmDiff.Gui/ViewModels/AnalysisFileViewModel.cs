using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Gui;

public sealed class AnalysisFileViewModel(
    string path,
    IReadOnlyList<DiffHunkModel> hunks,
    string groupTitle,
    string groupDescription,
    DiffGroupReviewModel? review)
{
    public string Path { get; } = path;
    public IReadOnlyList<DiffHunkModel> Hunks { get; } = hunks;
    public string GroupTitle { get; } = groupTitle;
    public string GroupDescription { get; } = groupDescription;
    public DiffGroupReviewModel? Review { get; } = review;
    public DiffRenderDocumentModel? RenderDocument { get; internal set; }

    public override string ToString()
    {
        return Path;
    }
}