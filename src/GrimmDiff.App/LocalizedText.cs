namespace GrimmDiff.App;

public sealed class LocalizedText(Func<string> language)
{
    private static readonly IReadOnlyDictionary<string, (string En, string De)> Values = new Dictionary<string, (string, string)>
    {
        ["Open"] = ("Open repository", "Repository öffnen"), ["Clone"] = ("Clone", "Klonen"), ["Fetch"] = ("Fetch", "Abrufen"), ["Pull"] = ("Pull", "Holen"), ["Push"] = ("Push", "Senden"),
        ["Repository"] = ("REPOSITORY", "REPOSITORY"), ["Branches"] = ("BRANCHES", "BRANCHES"), ["Recent"] = ("RECENT", "ZULETZT"), ["NewBranch"] = ("New branch", "Neuer Branch"),
        ["Changes"] = ("CHANGES", "ÄNDERUNGEN"), ["Working"] = ("Working tree", "Arbeitsbaum"), ["Staged"] = ("Staged", "Gestaged"), ["CommitMessage"] = ("COMMIT MESSAGE", "COMMIT-NACHRICHT"),
        ["Stage"] = ("Stage", "Stagen"), ["Unstage"] = ("Unstage", "Unstagen"), ["Commit"] = ("Commit", "Committen"), ["WorkSteps"] = ("WORK STEPS", "ARBEITSSCHRITTE")
    };
    public string this[string key] => Values.TryGetValue(key, out var value) ? (language() == "Deutsch" ? value.De : value.En) : key;
}
