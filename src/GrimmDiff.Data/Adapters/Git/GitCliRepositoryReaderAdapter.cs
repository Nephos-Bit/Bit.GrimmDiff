using GrimmDiff.Logic;
using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Data;

public sealed class GitCliRepositoryReaderAdapter(GitCliRepositoryService service) : IRepositoryReaderPort
{
    public Task<RepositorySnapshotModel> LoadAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return service.LoadAsync(repositoryPath, cancellationToken);
    }
}