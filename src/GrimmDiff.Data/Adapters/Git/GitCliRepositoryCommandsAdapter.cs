using GrimmDiff.Logic;
using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Data;

public sealed class GitCliRepositoryCommandsAdapter(GitCliRepositoryService service) : IRepositoryCommandsPort, IRepositoryClonerPort
{
    public Task<GitCommandResultModel> CloneAsync(string source, string destination,
        CancellationToken cancellationToken = default)
    {
        return service.CloneAsync(source, destination, cancellationToken);
    }

    public Task<GitCommandResultModel> StageAsync(string repositoryPath, string path,
        CancellationToken cancellationToken = default)
    {
        return service.StageAsync(repositoryPath, path, cancellationToken);
    }

    public Task<GitCommandResultModel> UnstageAsync(string repositoryPath, string path,
        CancellationToken cancellationToken = default)
    {
        return service.UnstageAsync(repositoryPath, path, cancellationToken);
    }

    public Task<GitCommandResultModel> CommitAsync(string repositoryPath, string message,
        CancellationToken cancellationToken = default)
    {
        return service.CommitAsync(repositoryPath, message, cancellationToken);
    }

    public Task<GitCommandResultModel> FetchAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return service.FetchAsync(repositoryPath, cancellationToken);
    }

    public Task<GitCommandResultModel> PullAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return service.PullAsync(repositoryPath, cancellationToken);
    }

    public Task<GitCommandResultModel> PushAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return service.PushAsync(repositoryPath, cancellationToken);
    }

    public Task<GitCommandResultModel> SwitchBranchAsync(string repositoryPath, string branch,
        CancellationToken cancellationToken = default)
    {
        return service.SwitchBranchAsync(repositoryPath, branch, cancellationToken);
    }

    public Task<GitCommandResultModel> CreateBranchAsync(string repositoryPath, string branch,
        CancellationToken cancellationToken = default)
    {
        return service.CreateBranchAsync(repositoryPath, branch, cancellationToken);
    }
}