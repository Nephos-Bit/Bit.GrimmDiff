namespace GrimmDiff.Application;

public sealed class CodexSessionService(ICodexSessionClient client)
{
    public Task<CodexCliStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return client.GetStatusAsync(cancellationToken);
    }

    public Task<CodexCliStatus> LoginAsync(CancellationToken cancellationToken = default)
    {
        return client.LoginAsync(cancellationToken);
    }

    public Task<CodexAccountUsage> GetAccountUsageAsync(CancellationToken cancellationToken = default)
    {
        return client.GetAccountUsageAsync(cancellationToken);
    }
}