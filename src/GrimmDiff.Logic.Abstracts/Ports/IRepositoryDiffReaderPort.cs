using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Logic.Abstracts;

public interface IRepositoryDiffReaderPort
{
    Task<string> GetDiffAsync(string repositoryPath, DiffScopeModel scope, string? path = null,
        CancellationToken cancellationToken = default);

    Task<string> GetCommitDiffAsync(string repositoryPath, string commitId, string? path = null,
        CancellationToken cancellationToken = default);
}