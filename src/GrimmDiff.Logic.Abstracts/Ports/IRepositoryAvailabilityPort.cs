namespace GrimmDiff.Logic.Abstracts.Ports;

public interface IRepositoryAvailabilityPort
{
    Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default);
}