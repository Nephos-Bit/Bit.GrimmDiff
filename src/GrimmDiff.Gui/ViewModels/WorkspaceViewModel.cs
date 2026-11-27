namespace GrimmDiff.Gui.ViewModels;

public sealed class WorkspaceViewModel
{
    public WorkspaceViewModel(RepositoryWorkspaceViewModel repository, AnalysisWorkspaceViewModel analysis,
        DiffDocumentViewModel diff, WorkspaceStatusViewModel status)
    {
        Repository = repository;
        Analysis = analysis;
        Diff = diff;
        Status = status;
        repository.ContextChanged += () => analysis.LoadAsync();
        repository.RefreshScheduled += analysis.Cancel;
    }

    public RepositoryWorkspaceViewModel Repository { get; }
    public AnalysisWorkspaceViewModel Analysis { get; }
    public DiffDocumentViewModel Diff { get; }
    public WorkspaceStatusViewModel Status { get; }
}
