using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Logic.Abstracts;

public interface IRepositoryCommandsPort
{
    Task<GitCommandResultModel> StageAsync(string repositoryPath, string path,
        CancellationToken cancellationToken = default);

    Task<GitCommandResultModel> UnstageAsync(string repositoryPath, string path,
        CancellationToken cancellationToken = default);

    Task<GitCommandResultModel> CommitAsync(string repositoryPath, string message,
        CancellationToken cancellationToken = default);

    Task<GitCommandResultModel> FetchAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<GitCommandResultModel> PullAsync(string repositoryPath, CancellationToken cancellationToken = default);
    Task<GitCommandResultModel> PushAsync(string repositoryPath, CancellationToken cancellationToken = default);

    Task<GitCommandResultModel> SwitchBranchAsync(string repositoryPath, string branch,
        CancellationToken cancellationToken = default);

    Task<GitCommandResultModel> CreateBranchAsync(string repositoryPath, string branch,
        CancellationToken cancellationToken = default);
}