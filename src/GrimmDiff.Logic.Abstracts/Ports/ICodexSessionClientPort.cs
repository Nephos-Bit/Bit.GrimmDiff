namespace GrimmDiff.Logic.Abstracts;

public interface ICodexSessionClientPort
{
    Task<CodexCliStatusModel> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<CodexCliStatusModel> LoginAsync(CancellationToken cancellationToken = default);

    Task<CodexAccountUsageModel> GetAccountUsageAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CodexAccountUsageModel(null, null));
    }
}