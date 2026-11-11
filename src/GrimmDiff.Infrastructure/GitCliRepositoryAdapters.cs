using GrimmDiff.Application;
using GrimmDiff.Domain;

namespace GrimmDiff.Infrastructure;

public sealed class GitCliRepositoryAvailability(GitCliRepositoryService service) : IRepositoryAvailability
{
    public Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default)
    {
        return service.IsGitAvailableAsync(cancellationToken);
    }
}

public sealed class GitCliRepositoryReader(GitCliRepositoryService service) : IRepositoryReader
{
    public Task<RepositorySnapshot> LoadAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return service.LoadAsync(repositoryPath, cancellationToken);
    }
}

public sealed class GitCliDiffReader(GitCliRepositoryService service) : IRepositoryDiffReader
{
    public Task<string> GetDiffAsync(string repositoryPath, DiffScope scope, string? path = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetDiffAsync(repositoryPath, scope, path, cancellationToken);
    }

    public Task<string> GetCommitDiffAsync(string repositoryPath, string commitId, string? path = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetCommitDiffAsync(repositoryPath, commitId, path, cancellationToken);
    }
}

public sealed class GitCliRepositoryCommands(GitCliRepositoryService service) : IRepositoryCommands, IRepositoryCloner
{
    public Task<GitCommandResult> CloneAsync(string source, string destination,
        CancellationToken cancellationToken = default)
    {
        return service.CloneAsync(source, destination, cancellationToken);
    }

    public Task<GitCommandResult> StageAsync(string repositoryPath, string path,
        CancellationToken cancellationToken = default)
    {
        return service.StageAsync(repositoryPath, path, cancellationToken);
    }

    public Task<GitCommandResult> UnstageAsync(string repositoryPath, string path,
        CancellationToken cancellationToken = default)
    {
        return service.UnstageAsync(repositoryPath, path, cancellationToken);
    }

    public Task<GitCommandResult> CommitAsync(string repositoryPath, string message,
        CancellationToken cancellationToken = default)
    {
        return service.CommitAsync(repositoryPath, message, cancellationToken);
    }

    public Task<GitCommandResult> FetchAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return service.FetchAsync(repositoryPath, cancellationToken);
    }

    public Task<GitCommandResult> PullAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return service.PullAsync(repositoryPath, cancellationToken);
    }

    public Task<GitCommandResult> PushAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return service.PushAsync(repositoryPath, cancellationToken);
    }

    public Task<GitCommandResult> SwitchBranchAsync(string repositoryPath, string branch,
        CancellationToken cancellationToken = default)
    {
        return service.SwitchBranchAsync(repositoryPath, branch, cancellationToken);
    }

    public Task<GitCommandResult> CreateBranchAsync(string repositoryPath, string branch,
        CancellationToken cancellationToken = default)
    {
        return service.CreateBranchAsync(repositoryPath, branch, cancellationToken);
    }
}