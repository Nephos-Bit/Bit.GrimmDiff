using System.Text.Json;
using System.IO;

namespace GrimmDiff.App;

public sealed record AppSettings(List<string> RecentRepositories, string Theme = "System", string Language = "English", string Font = "Cascadia Code")
{
    public static AppSettings Empty { get; } = new([]);
}
public sealed class AppSettingsStore
{
    private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GrimmDiff", "settings.json");
    public AppSettings Load()
    {
        try { return File.Exists(FilePath) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? AppSettings.Empty : AppSettings.Empty; }
        catch (JsonException) { return AppSettings.Empty; }
    }
    public void Save(IEnumerable<string> recent, string theme, string language, string font)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(new AppSettings(recent.ToList(), theme, language, font)));
    }
}
