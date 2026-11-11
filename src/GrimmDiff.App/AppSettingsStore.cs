using System.Text.Json;
using System.IO;
using GrimmDiff.Application;

namespace GrimmDiff.App;

public sealed class AppSettingsStore : IPreferencesStore
{
    private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GrimmDiff", "settings.json");
    public Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default)
    {
        try { return Task.FromResult(File.Exists(FilePath) ? JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(FilePath)) ?? AppPreferences.Empty : AppPreferences.Empty); }
        catch (JsonException) { return Task.FromResult(AppPreferences.Empty); }
    }
    public Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(preferences));
        return Task.CompletedTask;
    }
}
