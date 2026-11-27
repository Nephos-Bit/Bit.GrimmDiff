using GrimmDiff.Logic.Abstracts.Models;

namespace GrimmDiff.Logic.Abstracts.Ports;

public interface IPreferencesStorePort
{
    Task<AppPreferencesModel> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppPreferencesModel preferences, CancellationToken cancellationToken = default);
}