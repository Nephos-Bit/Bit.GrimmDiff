namespace GrimmDiff.Application;

public interface IRepositoryAvailability
{
    Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default);
}