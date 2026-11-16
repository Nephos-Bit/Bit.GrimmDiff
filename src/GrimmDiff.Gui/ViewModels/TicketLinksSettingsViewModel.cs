using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Gui;

public sealed class TicketLinksSettingsViewModel(MainViewModel main) :
    SettingsPageViewModelBase(main, "ticket-links", "Integrationen", "Ticket-Verlinkung", 0)
{
    public IReadOnlyList<TicketLinkProviderModel> Providers => Main.TicketLinkProviders;
    public string ConfigurationHint => Main.TicketLinkConfigurationHint;

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
