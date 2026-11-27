using GrimmDiff.Gui.Models;
using GrimmDiff.Gui.Services;

namespace GrimmDiff.Gui.ViewModels;

public sealed class MascotSettingsViewModel(PreferencesStateService preferences) :
    SettingsPageViewModelBase(preferences, "mascots", "Mascots", "Mascots", 0)
{
    public IReadOnlyList<MascotOption> Mascots => MascotCatalog.Options;

    public bool MascotsEnabled
    {
        get => Preferences.MascotsEnabled;
        set => Preferences.SetMascotsEnabled(value);
    }

    public MascotOption SelectedMascot
    {
        get => MascotCatalog.Find(Preferences.MascotId);
        set
        {
            if (value is not null) Preferences.SetMascotId(value.Id);
        }
    }
}
