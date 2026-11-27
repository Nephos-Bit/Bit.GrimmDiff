using GrimmDiff.Logic.Abstracts.Models;

namespace GrimmDiff.Logic.Abstracts.Ports;

public interface ICodexSessionClientPort
{
    Task<CodexCliStatusModel> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<CodexCliStatusModel> LoginAsync(CancellationToken cancellationToken = default);

    Task<CodexAccountUsageModel> GetAccountUsageAsync(CancellationToken cancellationToken = default);
}
