using GrimmDiff.Application;

namespace GrimmDiff.Infrastructure;

public sealed class GitCliRepositoryAvailability(GitCliRepositoryService service) : IRepositoryAvailability
{
    public Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default)
    {
        return service.IsGitAvailableAsync(cancellationToken);
    }
}