namespace GrimmDiff.App;

public sealed record ReleaseNotes(string Version, string German, string English)
{
    public string ForLanguage(string language) => language == "Deutsch" ? German : English;
}
