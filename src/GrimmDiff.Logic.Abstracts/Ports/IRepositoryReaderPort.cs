using GrimmDiff.Logic.Abstracts.Models;

namespace GrimmDiff.Logic.Abstracts.Ports;

public interface IRepositoryReaderPort
{
    Task<RepositorySnapshotModel> LoadAsync(string repositoryPath, CancellationToken cancellationToken = default);
}