using GrimmDiff.Application;
using GrimmDiff.Domain;

namespace GrimmDiff.Infrastructure;

public sealed class GitCliRepositoryReader(GitCliRepositoryService service) : IRepositoryReader
{
    public Task<RepositorySnapshot> LoadAsync(string repositoryPath, CancellationToken cancellationToken = default) =>
        service.LoadAsync(repositoryPath, cancellationToken);
}
