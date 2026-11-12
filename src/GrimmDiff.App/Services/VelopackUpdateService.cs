using Velopack;
using Velopack.Exceptions;
using Velopack.Sources;

namespace GrimmDiff.App;

public sealed class VelopackUpdateService : IApplicationUpdateService
{
    private static readonly Uri RepositoryUri = new("https://github.com/Nephos-Bit/Bit.GrimmDiff");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private UpdateInfo? _availableUpdate;

    public bool IsInstalled => new UpdateManager(new GithubSource(RepositoryUri.ToString(), null, false)).IsInstalled;

    public async Task<ApplicationUpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var manager = new UpdateManager(new GithubSource(RepositoryUri.ToString(), null, false));
            cancellationToken.ThrowIfCancellationRequested();
            _availableUpdate = await manager.CheckForUpdatesAsync();
            return _availableUpdate is null
                ? new ApplicationUpdateCheckResult(ApplicationUpdateStatus.NoUpdate)
                : new ApplicationUpdateCheckResult(ApplicationUpdateStatus.UpdateAvailable,
                    _availableUpdate.TargetFullRelease.Version.ToString());
        }
        catch (NotInstalledException)
        {
            return new ApplicationUpdateCheckResult(ApplicationUpdateStatus.NotInstalled);
        }
        catch (Exception)
        {
            return new ApplicationUpdateCheckResult(ApplicationUpdateStatus.Failed);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ApplicationUpdateApplyResult> DownloadAndRestartAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_availableUpdate is null) return ApplicationUpdateApplyResult.NoUpdateAvailable;
            var manager = new UpdateManager(new GithubSource(RepositoryUri.ToString(), null, false));
            cancellationToken.ThrowIfCancellationRequested();
            await manager.DownloadUpdatesAsync(_availableUpdate);
            manager.ApplyUpdatesAndRestart(_availableUpdate);
            return ApplicationUpdateApplyResult.Restarting;
        }
        catch (Exception)
        {
            return ApplicationUpdateApplyResult.Failed;
        }
        finally
        {
            _gate.Release();
        }
    }
}
