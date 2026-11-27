using GrimmDiff.Logic.Abstracts.Models;

namespace GrimmDiff.Logic.Abstracts.Ports;

public interface IRepositoryDiffReaderPort
{
    Task<string> GetDiffAsync(string repositoryPath, DiffScopeModel scope, string? path = null,
        CancellationToken cancellationToken = default);

    Task<string> GetCommitDiffAsync(string repositoryPath, string commitId, string? path = null,
        CancellationToken cancellationToken = default);
}