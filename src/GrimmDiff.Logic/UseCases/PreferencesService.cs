using GrimmDiff.Logic.Abstracts;
namespace GrimmDiff.Logic;

public sealed class PreferencesService(IPreferencesStorePort store)
{
    public Task<AppPreferencesModel> LoadAsync(CancellationToken cancellationToken = default)
    {
        return store.LoadAsync(cancellationToken);
    }

    public Task SaveAsync(AppPreferencesModel preferences, CancellationToken cancellationToken = default)
    {
        return store.SaveAsync(preferences, cancellationToken);
    }

    public AppPreferencesModel AddRecent(AppPreferencesModel preferences, string path)
    {
        return preferences with
        {
            RecentRepositories = preferences.RecentRepositories
                .Where(x => !string.Equals(x, path, StringComparison.OrdinalIgnoreCase)).Prepend(path).Take(10)
                .ToList()
        };
    }
}