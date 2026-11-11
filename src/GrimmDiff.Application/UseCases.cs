using GrimmDiff.Domain;

namespace GrimmDiff.Application;

public sealed class RepositoryWorkspaceService(IRepositoryAvailability availability, IRepositoryReader reader, IRepositoryCloner cloner)
{
    public Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default) => availability.IsGitAvailableAsync(cancellationToken);
    public Task<RepositorySnapshot> OpenAsync(string path, CancellationToken cancellationToken = default) => reader.LoadAsync(path, cancellationToken);
    public Task<GitCommandResult> CloneAsync(string source, string destination, CancellationToken cancellationToken = default) => cloner.CloneAsync(source, destination, cancellationToken);
}

public sealed record DiffAnalysisQuery(string RepositoryPath, string? CommitId, string Language, bool UseAi);
public sealed record DiffAnalysisView(DiffAnalysisResult Result, bool IsAiRequested);

public sealed class DiffAnalysisService(IRepositoryDiffReader diffs, IDiffAnalysisProvider aiProvider, IDiffAnalysisProvider heuristicProvider)
{
    public async Task<DiffAnalysisView> AnalyzeAsync(DiffAnalysisQuery query, CancellationToken cancellationToken = default)
    {
        var historical = !string.IsNullOrWhiteSpace(query.CommitId);
        var diff = historical
            ? await diffs.GetCommitDiffAsync(query.RepositoryPath, query.CommitId!, cancellationToken: cancellationToken)
            : (await diffs.GetDiffAsync(query.RepositoryPath, DiffScope.Staged, cancellationToken: cancellationToken))
              + (await diffs.GetDiffAsync(query.RepositoryPath, DiffScope.WorkingTree, cancellationToken: cancellationToken));
        var request = new DiffAnalysisRequest(query.RepositoryPath, historical ? DiffScope.Historical : DiffScope.WorkingTree, diff, query.Language);
        return new(await (query.UseAi ? aiProvider : heuristicProvider).AnalyzeAsync(request, cancellationToken), query.UseAi);
    }
}

public sealed class CodexSessionService(ICodexSessionClient client)
{
    public Task<CodexCliStatus> GetStatusAsync(CancellationToken cancellationToken = default) => client.GetStatusAsync(cancellationToken);
    public Task<CodexCliStatus> LoginAsync(CancellationToken cancellationToken = default) => client.LoginAsync(cancellationToken);
    public Task<CodexAccountUsage> GetAccountUsageAsync(CancellationToken cancellationToken = default) => client.GetAccountUsageAsync(cancellationToken);
}

public sealed class PreferencesService(IPreferencesStore store)
{
    public Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default) => store.LoadAsync(cancellationToken);
    public Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default) => store.SaveAsync(preferences, cancellationToken);
    public AppPreferences AddRecent(AppPreferences preferences, string path) => preferences with
    {
        RecentRepositories = preferences.RecentRepositories.Where(x => !string.Equals(x, path, StringComparison.OrdinalIgnoreCase)).Prepend(path).Take(10).ToList()
    };
}
