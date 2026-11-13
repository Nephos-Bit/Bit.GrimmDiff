namespace GrimmDiff.Logic.Abstracts;

public interface IPreferencesStorePort
{
    Task<AppPreferencesModel> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppPreferencesModel preferences, CancellationToken cancellationToken = default);
}