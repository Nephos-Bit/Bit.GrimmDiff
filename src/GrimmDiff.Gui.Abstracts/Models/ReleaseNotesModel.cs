namespace GrimmDiff.Gui.Abstracts.Models;

public sealed record ReleaseNotesModel(string Version, string German, string English)
{
    public string ForLanguage(string language)
    {
        return language == "Deutsch" ? German : English;
    }
}