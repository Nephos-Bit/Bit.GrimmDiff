using GrimmDiff.Logic.Abstracts.Models;

namespace GrimmDiff.Logic.Abstracts.Ports;

public interface IRepositoryCommandsPort
{
    Task<GitCommandResultModel> PullAsync(string repositoryPath, CancellationToken cancellationToken = default);

    Task<GitCommandResultModel> SwitchBranchAsync(string repositoryPath, string branch,
        CancellationToken cancellationToken = default);

}
