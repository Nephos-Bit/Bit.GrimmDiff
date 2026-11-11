namespace GrimmDiff.Application;
public interface ICodexSessionClient { Task<CodexCliStatus> GetStatusAsync(CancellationToken cancellationToken = default); Task<CodexCliStatus> LoginAsync(CancellationToken cancellationToken = default); Task<CodexAccountUsage> GetAccountUsageAsync(CancellationToken cancellationToken = default) => Task.FromResult(new CodexAccountUsage(null, null)); }
