using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.Abstracts.Ports;

namespace GrimmDiff.Logic.Tests;

internal sealed class FakeDiffReader(string staged, string working, string historical) : IRepositoryDiffReaderPort
{
    public Task<string> GetDiffAsync(string repositoryPath, DiffScopeModel scope, string? path = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(scope == DiffScopeModel.Staged ? staged : working);
    }

    public Task<string> GetCommitDiffAsync(string repositoryPath, string commitId, string? path = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(historical);
    }
}