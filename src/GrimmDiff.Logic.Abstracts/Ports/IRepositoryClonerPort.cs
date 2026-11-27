using GrimmDiff.Logic.Abstracts.Models;

namespace GrimmDiff.Logic.Abstracts.Ports;

public interface IRepositoryClonerPort
{
    Task<GitCommandResultModel> CloneAsync(string source, string destination, CancellationToken cancellationToken = default);
}