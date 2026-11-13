using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Logic;

public sealed class RepositoryWorkspaceService(
    IRepositoryAvailabilityPort availability,
    IRepositoryReaderPort reader,
    IRepositoryClonerPort cloner,
    IRepositoryCommandsPort commands)
{
    public Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default)
    {
        return availability.IsGitAvailableAsync(cancellationToken);
    }

    public Task<RepositorySnapshotModel> OpenAsync(string path, CancellationToken cancellationToken = default)
    {
        return reader.LoadAsync(path, cancellationToken);
    }

    public Task<GitCommandResultModel> CloneAsync(string source, string destination,
        CancellationToken cancellationToken = default)
    {
        return cloner.CloneAsync(source, destination, cancellationToken);
    }

    public Task<GitCommandResultModel> PullAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return commands.PullAsync(repositoryPath, cancellationToken);
    }

    public Task<GitCommandResultModel> SwitchBranchAsync(string repositoryPath, string branch,
        CancellationToken cancellationToken = default)
    {
        return commands.SwitchBranchAsync(repositoryPath, branch, cancellationToken);
    }
}