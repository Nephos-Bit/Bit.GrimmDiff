using GrimmDiff.Application;
using GrimmDiff.Domain;

namespace GrimmDiff.Infrastructure;

public sealed class GitCliDiffReader(GitCliRepositoryService service) : IRepositoryDiffReader
{
    public Task<string> GetDiffAsync(string repositoryPath, DiffScope scope, string? path = null,
        CancellationToken cancellationToken = default) => service.GetDiffAsync(repositoryPath, scope, path, cancellationToken);

    public Task<string> GetCommitDiffAsync(string repositoryPath, string commitId, string? path = null,
        CancellationToken cancellationToken = default) => service.GetCommitDiffAsync(repositoryPath, commitId, path, cancellationToken);
}
