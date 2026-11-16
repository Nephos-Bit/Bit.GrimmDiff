using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Gui;

public sealed class TicketLinksSettingsViewModel(MainViewModel main) :
    SettingsPageViewModelBase(main, "ticket-links", "Integrations", "TicketLinks", 0)
{
    public IReadOnlyList<TicketLinkProviderModel> Providers => Main.TicketLinkProviders;
    public string ConfigurationHint => Main.TicketLinkConfigurationHint;

    public string Pattern
    {
        get => Main.TicketLinkPattern;
        set => Main.TicketLinkPattern = value;
    }

    public TicketLinkProviderModel SelectedProvider
    {
        get => Main.SelectedTicketLinkProvider;
        set => Main.SelectedTicketLinkProvider = value;
    }

    public string BaseUrl
    {
        get => Main.TicketLinkBaseUrl;
        set => Main.TicketLinkBaseUrl = value;
    }
}
