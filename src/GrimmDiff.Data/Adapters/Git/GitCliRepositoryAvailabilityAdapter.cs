using GrimmDiff.Logic.Abstracts;
using GrimmDiff.Logic;

namespace GrimmDiff.Data;

public sealed class GitCliRepositoryAvailabilityAdapter(GitCliRepositoryService service) : IRepositoryAvailabilityPort
{
    public Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default)
    {
        return service.IsGitAvailableAsync(cancellationToken);
    }
}