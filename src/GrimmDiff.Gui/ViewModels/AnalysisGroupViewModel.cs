using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Gui;

public sealed class AnalysisGroupViewModel
{
    public AnalysisGroupViewModel(WorkStepGroupModel group)
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
    public DiffGroupReviewModel? Review { get; }
    public bool HasReview => Review is not null;
    public bool IsReviewGood => Review?.Status == DiffGroupReviewStatusModel.Good;
    public bool IsReviewOk => Review?.Status == DiffGroupReviewStatusModel.Ok;
    public bool IsReviewWarning => Review?.Status == DiffGroupReviewStatusModel.Warning;

    public string ReviewAccessibilityLabel => Review?.Status switch
    {
        DiffGroupReviewStatusModel.Good => "AI review: good",
        DiffGroupReviewStatusModel.Ok => "AI review: OK",
        DiffGroupReviewStatusModel.Warning => "AI review: warning",
        _ => string.Empty
    };
}