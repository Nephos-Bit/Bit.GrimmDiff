using GrimmDiff.Domain;

namespace GrimmDiff.App;

public sealed class AnalysisGroupViewModel
{
    public AnalysisGroupViewModel(WorkStepGroup group)
    {
        Title = group.Title;
        Description = group.Description;
        Files = group.Hunks.GroupBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase).Select(x =>
            new AnalysisFileViewModel(x.Key, x.ToList(), group.Title, group.Description, group.Review)).ToList();
        Review = group.Review;
    }

    public string Title { get; }
    public string Description { get; }
    public IReadOnlyList<AnalysisFileViewModel> Files { get; }
    public DiffGroupReview? Review { get; }
    public bool HasReview => Review is not null;
    public bool IsReviewGood => Review?.Status == DiffGroupReviewStatus.Good;
    public bool IsReviewOk => Review?.Status == DiffGroupReviewStatus.Ok;
    public bool IsReviewWarning => Review?.Status == DiffGroupReviewStatus.Warning;
    public string ReviewAccessibilityLabel => Review?.Status switch
    {
        DiffGroupReviewStatus.Good => "AI review: good",
        DiffGroupReviewStatus.Ok => "AI review: OK",
        DiffGroupReviewStatus.Warning => "AI review: warning",
        _ => string.Empty
    };
}
