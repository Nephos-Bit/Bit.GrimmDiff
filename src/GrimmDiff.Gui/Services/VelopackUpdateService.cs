using GrimmDiff.Logic.Abstracts;
using Velopack;
using Velopack.Exceptions;
using Velopack.Sources;

namespace GrimmDiff.Gui;

public sealed class VelopackUpdateService : IApplicationUpdateService
{
    private static readonly Uri RepositoryUri = new("https://github.com/Nephos-Bit/Bit.GrimmDiff");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private UpdateInfo? _availableUpdate;

    public bool IsInstalled => new UpdateManager(new GithubSource(RepositoryUri.ToString(), null, false)).IsInstalled;

    public async Task<ApplicationUpdateCheckResultModel> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var manager = new UpdateManager(new GithubSource(RepositoryUri.ToString(), null, false));
            cancellationToken.ThrowIfCancellationRequested();
            _availableUpdate = await manager.CheckForUpdatesAsync();
            return _availableUpdate is null
                ? new ApplicationUpdateCheckResultModel(ApplicationUpdateStatusModel.NoUpdate)
                : new ApplicationUpdateCheckResultModel(ApplicationUpdateStatusModel.UpdateAvailable,
                    _availableUpdate.TargetFullRelease.Version.ToString());
        }
        catch (NotInstalledException)
        {
            return new ApplicationUpdateCheckResultModel(ApplicationUpdateStatusModel.NotInstalled);
        }
        catch (Exception)
        {
            return new ApplicationUpdateCheckResultModel(ApplicationUpdateStatusModel.Failed);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ApplicationUpdateApplyResultModel> DownloadAndRestartAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_availableUpdate is null) return ApplicationUpdateApplyResultModel.NoUpdateAvailable;
            var manager = new UpdateManager(new GithubSource(RepositoryUri.ToString(), null, false));
            cancellationToken.ThrowIfCancellationRequested();
            await manager.DownloadUpdatesAsync(_availableUpdate);
            manager.ApplyUpdatesAndRestart(_availableUpdate);
            return ApplicationUpdateApplyResultModel.Restarting;
        }
        catch (Exception)
        {
            return ApplicationUpdateApplyResultModel.Failed;
        }
        finally
        {
            _gate.Release();
        }
    }
}