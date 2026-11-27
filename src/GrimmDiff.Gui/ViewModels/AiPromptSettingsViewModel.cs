using System.Globalization;
using System.Text.Json;
using GrimmDiff.Data.Adapters.Codex;
using GrimmDiff.Gui.Services;
using GrimmDiff.Logic.Abstracts.Policies;

namespace GrimmDiff.Gui.ViewModels;

public sealed class AiPromptSettingsViewModel : SettingsPageViewModelBase
{
    public AiPromptSettingsViewModel(PreferencesStateService preferences)
        : base(preferences, "ai-prompt", "Ai", "AiPrompt", 2)
    {
        preferences.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(PreferencesStateService.Language) or "")
            {
                OnPropertyChanged(nameof(LimitsDescription));
                OnPropertyChanged(nameof(MaximumSectionCharacters));
                OnPropertyChanged(nameof(MaximumTransferCharacters));
                OnPropertyChanged(nameof(ResponseExample));
            }
        };
    }

    public string LimitsDescription => string.Format(NumberCulture, Text["AiWikiLimitsTemplate"],
        AiDiffSafetyPolicy.MaximumRemoteHunkCharacters,
        AiDiffSafetyPolicy.MaximumRemoteCharacters,
        CodexCliDiffAnalysisAdapter.DefaultChunkCharacterLimit,
        CodexCliDiffAnalysisAdapter.DefaultMaxChunks);

    public string MaximumSectionCharacters =>
        AiDiffSafetyPolicy.MaximumRemoteHunkCharacters.ToString("N0", NumberCulture);

    public string MaximumTransferCharacters =>
        AiDiffSafetyPolicy.MaximumRemoteCharacters.ToString("N0", NumberCulture);

    public int MaximumParts => CodexCliDiffAnalysisAdapter.DefaultMaxChunks;

    private CultureInfo NumberCulture => CultureInfo.GetCultureInfo(Preferences.Language == "Deutsch" ? "de-DE" : "en-US");

    public string ResponseExample => JsonSerializer.Serialize(new
    {
        groups = new[]
        {
            new
            {
                title = Preferences.Language == "Deutsch" ? "Validierung ergänzen" : "Add validation",
                description = Preferences.Language == "Deutsch"
                    ? "Ergänzt die Prüfung ungültiger Eingaben."
                    : "Adds checks for invalid input.",
                hunkIds = new[] { "H0001" },
                confidence = 0.85,
                reviewStatus = "ok",
                reviewHint = Preferences.Language == "Deutsch"
                    ? "Den Grenzfall mit leerer Eingabe prüfen."
                    : "Check the empty-input edge case."
            }
        }
    }, new JsonSerializerOptions { WriteIndented = true });
}
