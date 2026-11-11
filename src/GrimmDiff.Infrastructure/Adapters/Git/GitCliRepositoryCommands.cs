using GrimmDiff.Application;
using GrimmDiff.Domain;

namespace GrimmDiff.Infrastructure;

public sealed class GitCliRepositoryCommands(GitCliRepositoryService service) : IRepositoryCommands, IRepositoryCloner
{
    public Task<GitCommandResult> CloneAsync(string source, string destination, CancellationToken cancellationToken = default) => service.CloneAsync(source, destination, cancellationToken);
    public Task<GitCommandResult> StageAsync(string repositoryPath, string path, CancellationToken cancellationToken = default) => service.StageAsync(repositoryPath, path, cancellationToken);
    public Task<GitCommandResult> UnstageAsync(string repositoryPath, string path, CancellationToken cancellationToken = default) => service.UnstageAsync(repositoryPath, path, cancellationToken);
    public Task<GitCommandResult> CommitAsync(string repositoryPath, string message, CancellationToken cancellationToken = default) => service.CommitAsync(repositoryPath, message, cancellationToken);
    public Task<GitCommandResult> FetchAsync(string repositoryPath, CancellationToken cancellationToken = default) => service.FetchAsync(repositoryPath, cancellationToken);
    public Task<GitCommandResult> PullAsync(string repositoryPath, CancellationToken cancellationToken = default) => service.PullAsync(repositoryPath, cancellationToken);
    public Task<GitCommandResult> PushAsync(string repositoryPath, CancellationToken cancellationToken = default) => service.PushAsync(repositoryPath, cancellationToken);
    public Task<GitCommandResult> SwitchBranchAsync(string repositoryPath, string branch, CancellationToken cancellationToken = default) => service.SwitchBranchAsync(repositoryPath, branch, cancellationToken);
    public Task<GitCommandResult> CreateBranchAsync(string repositoryPath, string branch, CancellationToken cancellationToken = default) => service.CreateBranchAsync(repositoryPath, branch, cancellationToken);
}
