using GrimmDiff.Data.Services;
using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.Abstracts.Ports;


namespace GrimmDiff.Data.Adapters.Git;

public sealed class GitCliRepositoryAvailabilityAdapter : IRepositoryAvailabilityPort
{
    public async Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default)
    {
        return (await GitCliProcessRunner.RunAsync(null, ["--version"], cancellationToken)).IsSuccess;
    }
}
