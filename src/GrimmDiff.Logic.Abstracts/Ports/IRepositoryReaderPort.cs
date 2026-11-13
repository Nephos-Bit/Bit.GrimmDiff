using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Logic.Abstracts;

public interface IRepositoryReaderPort
{
    Task<RepositorySnapshotModel> LoadAsync(string repositoryPath, CancellationToken cancellationToken = default);
}