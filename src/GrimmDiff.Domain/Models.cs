namespace GrimmDiff.Domain;

public enum DiffScope { WorkingTree, Staged, Historical }
public enum DiffAnalysisSource { Codex, Heuristic }
public enum ChangeKind { Added, Modified, Deleted, Renamed, Untracked, Conflicted }
public enum RepositoryOperation { Stage, Unstage, Commit, Fetch, Pull, Push, SwitchBranch, CreateBranch }
public enum RepositoryState { Ready, Conflict, MergeInProgress, RebaseInProgress, CherryPickInProgress, Unavailable }

public sealed record GitFileChange(string Path, ChangeKind Kind, DiffScope Scope);
public sealed record BranchInfo(string Name, bool IsCurrent);
public sealed record CommitInfo(string Id, string Subject, string Author, DateTimeOffset Date);
public sealed record RepositorySnapshot(
    string Path, string Branch, RepositoryState State, IReadOnlyList<GitFileChange> Changes,
    IReadOnlyList<BranchInfo> Branches, IReadOnlyList<CommitInfo> History);
public sealed record DiffHunk(string FilePath, DiffScope Scope, string Header, string Content, int OldStart, int NewStart);
public sealed record DiffAnalysisRequest(string RepositoryPath, DiffScope Scope, string Diff, string Language);
public sealed record WorkStepGroup(string Title, string Description, IReadOnlyList<DiffHunk> Hunks, double Confidence);
public sealed record DiffAnalysisResult(
    IReadOnlyList<DiffHunk> Hunks,
    IReadOnlyList<WorkStepGroup> Groups,
    DiffAnalysisSource Source,
    string? Diagnostic = null);
public sealed record GitCommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool IsSuccess => ExitCode == 0;
    public string ErrorMessage => string.IsNullOrWhiteSpace(StandardError) ? StandardOutput : StandardError;
}

public interface IDiffAnalysisProvider
{
    Task<DiffAnalysisResult> AnalyzeAsync(DiffAnalysisRequest request, CancellationToken cancellationToken = default);
}

public interface IGitRepositoryService
{
    Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default);
    Task<RepositorySnapshot> LoadAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<string> GetDiffAsync(string repositoryPath, DiffScope scope, string? path = null, CancellationToken cancellationToken = default);
    Task<string> GetCommitDiffAsync(string repositoryPath, string commitId, string? path = null, CancellationToken cancellationToken = default);
    Task<GitCommandResult> StageAsync(string repositoryPath, string path, CancellationToken cancellationToken = default);
    Task<GitCommandResult> UnstageAsync(string repositoryPath, string path, CancellationToken cancellationToken = default);
    Task<GitCommandResult> CommitAsync(string repositoryPath, string message, CancellationToken cancellationToken = default);
    Task<GitCommandResult> FetchAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<GitCommandResult> PullAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<GitCommandResult> PushAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<GitCommandResult> SwitchBranchAsync(string repositoryPath, string branch, CancellationToken cancellationToken = default);
    Task<GitCommandResult> CreateBranchAsync(string repositoryPath, string branch, CancellationToken cancellationToken = default);
    Task<GitCommandResult> CloneAsync(string source, string destination, CancellationToken cancellationToken = default);
}
