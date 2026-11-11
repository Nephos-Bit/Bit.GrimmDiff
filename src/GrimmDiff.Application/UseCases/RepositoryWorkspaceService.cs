using GrimmDiff.Domain;

namespace GrimmDiff.Application;

public sealed class RepositoryWorkspaceService(
    IRepositoryAvailability availability,
    IRepositoryReader reader,
    IRepositoryCloner cloner)
{
    public Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default)
    {
        return availability.IsGitAvailableAsync(cancellationToken);
    }

    public Task<RepositorySnapshot> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        return reader.LoadAsync(path, cancellationToken);
    }

    public Task<GitCommandResult> CloneAsync(string source, string destination,
        CancellationToken cancellationToken = default)
    {
        return cloner.CloneAsync(source, destination, cancellationToken);
    }
}