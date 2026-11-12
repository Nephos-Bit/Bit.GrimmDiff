namespace GrimmDiff.App;

public interface IApplicationUpdateService
{
    bool IsInstalled { get; }
    Task<ApplicationUpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);
    Task<ApplicationUpdateApplyResult> DownloadAndRestartAsync(CancellationToken cancellationToken = default);
}
