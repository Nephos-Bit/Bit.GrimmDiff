using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Logic.Abstracts;

public interface IRepositoryClonerPort
{
    Task<GitCommandResultModel> CloneAsync(string source, string destination, CancellationToken cancellationToken = default);
}