using GrimmDiff.Domain;

namespace GrimmDiff.Application;

public interface IRepositoryCommands
{
    Task<GitCommandResult> StageAsync(string repositoryPath, string path,
        CancellationToken cancellationToken = default);

    Task<GitCommandResult> UnstageAsync(string repositoryPath, string path,
        CancellationToken cancellationToken = default);

    Task<GitCommandResult> CommitAsync(string repositoryPath, string message,
        CancellationToken cancellationToken = default);

    Task<GitCommandResult> FetchAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<GitCommandResult> PullAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<GitCommandResult> PushAsync(string repositoryPath, CancellationToken cancellationToken = default);

    Task<GitCommandResult> SwitchBranchAsync(string repositoryPath, string branch,
        CancellationToken cancellationToken = default);

    Task<GitCommandResult> CreateBranchAsync(string repositoryPath, string branch,
        CancellationToken cancellationToken = default);
}