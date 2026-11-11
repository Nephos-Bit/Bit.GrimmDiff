using GrimmDiff.Domain;

namespace GrimmDiff.Application;

public interface IRepositoryDiffReader
{
    Task<string> GetDiffAsync(string repositoryPath, DiffScope scope, string? path = null,
        CancellationToken cancellationToken = default);

    Task<string> GetCommitDiffAsync(string repositoryPath, string commitId, string? path = null,
        CancellationToken cancellationToken = default);
}