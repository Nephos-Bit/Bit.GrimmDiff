namespace GrimmDiff.App;

public sealed record ReleaseNotes(string Version, string German, string English)
{
    public string ForLanguage(string language)
    {
        return language == "Deutsch" ? German : English;
    }
}