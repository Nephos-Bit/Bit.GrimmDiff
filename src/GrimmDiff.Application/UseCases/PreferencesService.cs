namespace GrimmDiff.Application;

public sealed class PreferencesService(IPreferencesStore store)
{
    public Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default)
    {
        return store.LoadAsync(cancellationToken);
    }

    public Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default)
    {
        return store.SaveAsync(preferences, cancellationToken);
    }

    public AppPreferences AddRecent(AppPreferences preferences, string path)
    {
        return preferences with
        {
            RecentRepositories = preferences.RecentRepositories
                .Where(x => !string.Equals(x, path, StringComparison.OrdinalIgnoreCase)).Prepend(path).Take(10)
                .ToList()
        };
    }
}