using GrimmDiff.Domain;

namespace GrimmDiff.Application;

public sealed class RepositoryWorkspaceService(
    IRepositoryAvailability availability,
    IRepositoryReader reader,
    IRepositoryCloner cloner,
    IRepositoryCommands commands)
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

    public Task<GitCommandResult> PullAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return commands.PullAsync(repositoryPath, cancellationToken);
    }

    public Task<GitCommandResult> SwitchBranchAsync(string repositoryPath, string branch,
        CancellationToken cancellationToken = default)
    {
        return commands.SwitchBranchAsync(repositoryPath, branch, cancellationToken);
    }
}