using GrimmDiff.Domain;

namespace GrimmDiff.Application;

public interface IRepositoryReader
{
    Task<RepositorySnapshot> LoadAsync(string repositoryPath, CancellationToken cancellationToken = default);
}