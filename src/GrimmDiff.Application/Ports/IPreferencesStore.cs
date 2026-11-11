namespace GrimmDiff.Application;
public interface IPreferencesStore { Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default); Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default); }
