namespace GrimmDiff.Application;
public sealed record AppPreferences(IReadOnlyList<string> RecentRepositories, string Theme = "System", string Language = "English", string Font = "Cascadia Code") { public static AppPreferences Empty { get; } = new([]); }
