using GrimmDiff.Domain;

namespace GrimmDiff.Application;

public interface IRepositoryCloner
{
    Task<GitCommandResult> CloneAsync(string source, string destination, CancellationToken cancellationToken = default);
}