using GrimmDiff.Domain;

namespace GrimmDiff.App;

public sealed class AnalysisGroupViewModel
{
    public AnalysisGroupViewModel(WorkStepGroup group)
    {
        Title = group.Title;
        Description = group.Description;
        Files = group.Hunks.GroupBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase).Select(x =>
            new AnalysisFileViewModel(x.Key, x.ToList(), group.Title, group.Description)).ToList();
    }

    public string Title { get; }
    public string Description { get; }
    public IReadOnlyList<AnalysisFileViewModel> Files { get; }
}
