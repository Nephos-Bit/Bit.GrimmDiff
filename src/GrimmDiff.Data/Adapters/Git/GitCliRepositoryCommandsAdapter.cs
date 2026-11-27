using GrimmDiff.Data.Services;
using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.Abstracts.Ports;


namespace GrimmDiff.Data.Adapters.Git;

public sealed class GitCliRepositoryCommandsAdapter : IRepositoryCommandsPort, IRepositoryClonerPort
{
    public Task<GitCommandResultModel> CloneAsync(string source, string destination,
        CancellationToken cancellationToken = default)
    {
        return GitCliProcessRunner.RunAsync(null, ["clone", source, destination], cancellationToken);
    }

    public Task<GitCommandResultModel> PullAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return GitCliProcessRunner.RunAsync(repositoryPath, ["pull"], cancellationToken);
    }

    public Task<GitCommandResultModel> SwitchBranchAsync(string repositoryPath, string branch,
        CancellationToken cancellationToken = default)
    {
        return GitCliProcessRunner.RunAsync(repositoryPath, ["switch", branch], cancellationToken);
    }

}
