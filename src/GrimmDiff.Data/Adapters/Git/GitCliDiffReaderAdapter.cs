using GrimmDiff.Logic;
using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Data;

public sealed class GitCliDiffReaderAdapter(GitCliRepositoryService service) : IRepositoryDiffReaderPort
{
    public Task<string> GetDiffAsync(string repositoryPath, DiffScopeModel scope, string? path = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetDiffAsync(repositoryPath, scope, path, cancellationToken);
    }

    public Task<string> GetCommitDiffAsync(string repositoryPath, string commitId, string? path = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetCommitDiffAsync(repositoryPath, commitId, path, cancellationToken);
    }
}