using GrimmDiff.Gui.Services;
using GrimmDiff.Gui.Models;
using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Gui.ViewModels;

public sealed class TicketLinksSettingsViewModel : SettingsPageViewModelBase
{
    public TicketLinksSettingsViewModel(PreferencesStateService preferences) :
        base(preferences, "ticket-links", "Integrations", "TicketLinks", 0)
    {
        preferences.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(PreferencesStateService.TicketLinks) or "")
            {
                OnPropertyChanged(nameof(SelectedProvider));
                OnPropertyChanged(nameof(BaseUrl));
                OnPropertyChanged(nameof(Pattern));
                OnPropertyChanged(nameof(ConfigurationHint));
                OnPropertyChanged(nameof(HasValidBaseUrl));
                OnPropertyChanged(nameof(HasValidPattern));
            }
            if (args.PropertyName == nameof(PreferencesStateService.Language))
                OnPropertyChanged(nameof(ConfigurationHint));
        };
    }

    public IReadOnlyList<TicketLinkProviderModel> Providers { get; } = Enum.GetValues<TicketLinkProviderModel>();
    public TicketLinkProviderModel SelectedProvider
    {
        get => Preferences.TicketLinks.Provider;
        set => Preferences.SetTicketLinks(Preferences.TicketLinks with { Provider = value });
    }
    public string BaseUrl
    {
        get => Preferences.TicketLinks.BaseUrl ?? string.Empty;
        set
        {
            var normalized = value.Trim();
            Preferences.SetTicketLinks(Preferences.TicketLinks with
            {
                BaseUrl = string.IsNullOrWhiteSpace(normalized) ? null : normalized
            });
        }
    }
    public string Pattern
    {
        get => Preferences.TicketLinks.Pattern ?? string.Empty;
        set
        {
            var normalized = value.Trim();
            Preferences.SetTicketLinks(Preferences.TicketLinks with
            {
                Pattern = string.IsNullOrWhiteSpace(normalized) ? null : normalized
            });
        }
    }
    public bool HasValidBaseUrl => SelectedProvider == TicketLinkProviderModel.Disabled ||
                                   TicketLinkResolver.IsValidBaseUrl(SelectedProvider, BaseUrl);
    public bool HasValidPattern => TicketLinkResolver.IsValidPattern(SelectedProvider, Pattern);
    public string ConfigurationHint => !HasValidBaseUrl
        ? SelectedProvider == TicketLinkProviderModel.Custom
            ? Localize("Custom requires an HTTPS URL without credentials, a non-standard port, query parameters, or a fragment.",
                "Custom benötigt eine HTTPS-URL ohne Zugangsdaten, abweichenden Port, Parameter oder Fragment.")
            : Localize("Only official provider hosts without credentials, a port, query parameters, or a fragment are allowed.",
                "Nur offizielle Anbieter-Hosts ohne Zugangsdaten, Port, Parameter oder Fragment sind zulässig.")
        : !HasValidPattern
            ? Localize("The pattern must contain a named (?<id>...) group and be a valid regular expression.",
                "Das Muster muss eine benannte Gruppe (?<id>...) enthalten und ein gültiger regulärer Ausdruck sein.")
            : SelectedProvider switch
            {
                TicketLinkProviderModel.Disabled => Localize("Ticket linking is disabled.", "Ticket-Verlinkung ist deaktiviert."),
                TicketLinkProviderModel.Jira => Localize("Base URL of the Jira instance, for example https://company.atlassian.net",
                    "Basis-URL der Jira-Instanz, z. B. https://firma.atlassian.net"),
                TicketLinkProviderModel.GitHub => Localize("Base URL of the GitHub project, for example https://github.com/org/repository",
                    "Basis-URL des GitHub-Projekts, z. B. https://github.com/org/repository"),
                TicketLinkProviderModel.GitLab => Localize("Base URL of the GitLab project, for example https://gitlab.com/group/project",
                    "Basis-URL des GitLab-Projekts, z. B. https://gitlab.com/gruppe/projekt"),
                TicketLinkProviderModel.AzureDevOps => Localize("Base URL of the Azure DevOps project, for example https://dev.azure.com/organization/project",
                    "Basis-URL des Azure-DevOps-Projekts, z. B. https://dev.azure.com/organisation/projekt"),
                TicketLinkProviderModel.Custom => Localize("Custom HTTPS base URL, including an internal host name; the detected id is appended to it.",
                    "Frei wählbare HTTPS-Basis-URL, auch mit internem Hostnamen; die erkannte id wird daran angehängt."),
                _ => string.Empty
            };
    private string Localize(string english, string german) => Preferences.Language == "Deutsch" ? german : english;
}
