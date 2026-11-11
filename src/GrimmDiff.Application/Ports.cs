using GrimmDiff.Domain;

namespace GrimmDiff.Application;

public interface IRepositoryAvailability
{
    Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default);
}

public interface IRepositoryReader
{
    Task<RepositorySnapshot> LoadAsync(string repositoryPath, CancellationToken cancellationToken = default);
}

public interface IRepositoryDiffReader
{
    Task<string> GetDiffAsync(string repositoryPath, DiffScope scope, string? path = null, CancellationToken cancellationToken = default);
    Task<string> GetCommitDiffAsync(string repositoryPath, string commitId, string? path = null, CancellationToken cancellationToken = default);
}

public interface IRepositoryCommands
{
    Task<GitCommandResult> StageAsync(string repositoryPath, string path, CancellationToken cancellationToken = default);
    Task<GitCommandResult> UnstageAsync(string repositoryPath, string path, CancellationToken cancellationToken = default);
    Task<GitCommandResult> CommitAsync(string repositoryPath, string message, CancellationToken cancellationToken = default);
    Task<GitCommandResult> FetchAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<GitCommandResult> PullAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<GitCommandResult> PushAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<GitCommandResult> SwitchBranchAsync(string repositoryPath, string branch, CancellationToken cancellationToken = default);
    Task<GitCommandResult> CreateBranchAsync(string repositoryPath, string branch, CancellationToken cancellationToken = default);
}

public interface IRepositoryCloner
{
    Task<GitCommandResult> CloneAsync(string source, string destination, CancellationToken cancellationToken = default);
}

public interface IDiffAnalysisProvider
{
    Task<DiffAnalysisResult> AnalyzeAsync(DiffAnalysisRequest request, CancellationToken cancellationToken = default);
}

public interface ICodexSessionClient
{
    Task<CodexCliStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<CodexCliStatus> LoginAsync(CancellationToken cancellationToken = default);
    Task<CodexAccountUsage> GetAccountUsageAsync(CancellationToken cancellationToken = default) => Task.FromResult(new CodexAccountUsage(null, null));
}

public interface ICodexStructuredRunner : ICodexSessionClient
{
    Task<string> RunStructuredAsync(string repositoryPath, string prompt, string schema, TimeSpan timeout, CancellationToken cancellationToken = default);
}

public interface IPreferencesStore
{
    Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default);
}

public sealed record AppPreferences(IReadOnlyList<string> RecentRepositories, string Theme = "System", string Language = "English", string Font = "Cascadia Code")
{
    public static AppPreferences Empty { get; } = new([]);
}

public enum CodexCliStatusKind { Ready, Missing, NotAuthenticated, ApiKeyAuthentication, UnsupportedAuthentication, Unavailable }
public sealed record CodexCliStatus(CodexCliStatusKind Kind, string Message) { public bool IsReady => Kind == CodexCliStatusKind.Ready; }
public sealed record CodexAccountUsage(string? Email, int? WeeklyRemainingPercent);
