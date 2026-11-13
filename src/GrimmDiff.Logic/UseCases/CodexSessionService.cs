using GrimmDiff.Logic.Abstracts;
namespace GrimmDiff.Logic;

public sealed class CodexSessionService(ICodexSessionClientPort client)
{
    public Task<CodexCliStatusModel> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return client.GetStatusAsync(cancellationToken);
    }

    public Task<CodexCliStatusModel> LoginAsync(CancellationToken cancellationToken = default)
    {
        return client.LoginAsync(cancellationToken);
    }

    public Task<CodexAccountUsageModel> GetAccountUsageAsync(CancellationToken cancellationToken = default)
    {
        return client.GetAccountUsageAsync(cancellationToken);
    }
}