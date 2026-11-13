namespace GrimmDiff.Logic.Abstracts;

public interface IRepositoryAvailabilityPort
{
    Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default);
}