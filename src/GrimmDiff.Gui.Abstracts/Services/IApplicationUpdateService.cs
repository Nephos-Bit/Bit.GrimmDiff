namespace GrimmDiff.Gui.Abstracts;

public interface IApplicationUpdateService
{
    bool IsInstalled { get; }
    Task<ApplicationUpdateCheckResultModel> CheckForUpdateAsync(CancellationToken cancellationToken = default);
    Task<ApplicationUpdateApplyResultModel> DownloadAndRestartAsync(CancellationToken cancellationToken = default);
}