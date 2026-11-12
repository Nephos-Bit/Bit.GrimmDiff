namespace GrimmDiff.App;

public interface IApplicationUpdateService
{
    Task<ApplicationUpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);
    Task<ApplicationUpdateApplyResult> DownloadAndRestartAsync(CancellationToken cancellationToken = default);
}
