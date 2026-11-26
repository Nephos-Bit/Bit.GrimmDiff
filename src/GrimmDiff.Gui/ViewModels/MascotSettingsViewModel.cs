namespace GrimmDiff.Gui;

public sealed class MascotSettingsViewModel(MainViewModel main) :
    SettingsPageViewModelBase(main, "mascots", "Mascots", "Mascots", 0)
{
    public IReadOnlyList<MascotOption> Mascots => MascotCatalog.Options;

    public bool MascotsEnabled
    {
        get => Main.MascotsEnabled;
        set => Main.MascotsEnabled = value;
    }

    public MascotOption SelectedMascot
    {
        get => MascotCatalog.Find(Main.SelectedMascotId);
        set
        {
            if (value is not null) Main.SelectedMascotId = value.Id;
        }
    }
}
