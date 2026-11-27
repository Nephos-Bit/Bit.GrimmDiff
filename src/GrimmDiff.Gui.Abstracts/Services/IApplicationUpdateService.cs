using GrimmDiff.Gui.Abstracts.Models;

namespace GrimmDiff.Gui.Abstracts.Services;

public interface IApplicationUpdateService
{
    bool IsInstalled { get; }
    Task<ApplicationUpdateCheckResultModel> CheckForUpdateAsync(CancellationToken cancellationToken = default);
    Task<ApplicationUpdateApplyResultModel> DownloadAndRestartAsync(CancellationToken cancellationToken = default);
}