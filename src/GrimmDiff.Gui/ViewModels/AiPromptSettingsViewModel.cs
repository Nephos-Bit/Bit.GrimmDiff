using GrimmDiff.Gui.Services;
namespace GrimmDiff.Gui.ViewModels;

public sealed class AiPromptSettingsViewModel(PreferencesStateService preferences) :
    SettingsPageViewModelBase(preferences, "ai-prompt", "Ai", "AiPrompt", 2)
{
}
