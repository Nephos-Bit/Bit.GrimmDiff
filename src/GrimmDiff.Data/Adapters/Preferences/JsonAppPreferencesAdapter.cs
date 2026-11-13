using GrimmDiff.Logic.Abstracts;
using System.IO;
using System.Text.Json;
using GrimmDiff.Logic;

namespace GrimmDiff.Gui;

public sealed class JsonAppPreferencesAdapter : IPreferencesStorePort
{
    private static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GrimmDiff",
            "settings.json");

    public Task<AppPreferencesModel> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(File.Exists(FilePath)
                ? JsonSerializer.Deserialize<AppPreferencesModel>(File.ReadAllText(FilePath)) ?? AppPreferencesModel.Empty
                : AppPreferencesModel.Empty);
        }
        catch (JsonException)
        {
            return Task.FromResult(AppPreferencesModel.Empty);
        }
    }

    public Task SaveAsync(AppPreferencesModel preferences, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(preferences));
        return Task.CompletedTask;
    }
}