using GrimmDiff.Logic.Abstracts;
namespace GrimmDiff.Gui;

public sealed class LocalizedTextService(Func<string> language)
{
    private static readonly IReadOnlyDictionary<string, (string En, string De)> Values =
        new Dictionary<string, (string, string)>
        {
            ["Open"] = ("Open repository", "Repository öffnen"), ["Clone"] = ("Clone", "Klonen"),
            ["Fetch"] = ("Fetch", "Abrufen"), ["Pull"] = ("Pull", "Holen"), ["Push"] = ("Push", "Senden"),
            ["Repository"] = ("REPOSITORY", "REPOSITORY"), ["Branches"] = ("BRANCHES", "BRANCHES"),
            ["Branch"] = ("Branch", "Branch"), ["NoBranch"] = ("No branch selected", "Kein Branch ausgewählt"),
            ["Recent"] = ("RECENT", "ZULETZT"), ["NewBranch"] = ("New branch", "Neuer Branch"),
            ["Changes"] = ("CHANGES", "ÄNDERUNGEN"), ["Working"] = ("Working tree", "Arbeitsbaum"),
            ["Staged"] = ("Staged", "Gestaged"), ["CommitMessage"] = ("COMMIT MESSAGE", "COMMIT-NACHRICHT"),
            ["Stage"] = ("Stage", "Stagen"), ["Unstage"] = ("Unstage", "Unstagen"),
            ["Commit"] = ("Commit", "Committen"), ["WorkSteps"] = ("WORK STEPS", "ARBEITSSCHRITTE"),
            ["General"] = ("General", "Allgemein"), ["Appearance"] = ("Appearance", "Darstellung"),
            ["Ai"] = ("AI", "KI"), ["Account"] = ("Account", "Konto"),
            ["PrivacyAndApprovals"] = ("Privacy & approvals", "Datenschutz & Freigaben"),
            ["AiPrompt"] = ("CLI prompt", "CLI-Prompt"),
            ["FixedGroupingInstructions"] = ("FIXED GROUPING INSTRUCTIONS", "FESTE ANWEISUNGEN ZUR GRUPPIERUNG"),
            ["Integrations"] = ("Integrations", "Integrationen"),
            ["TicketLinks"] = ("Ticket links", "Ticket-Verlinkung"),
            ["About"] = ("About", "Über"), ["UpdatesAndInfo"] = ("Updates & info", "Updates & Info"),
            ["Language"] = ("LANGUAGE", "SPRACHE"), ["Theme"] = ("THEME", "THEME"),
            ["Font"] = ("FONT", "SCHRIFT"), ["Status"] = ("STATUS", "STATUS"),
            ["SignInWithChatGpt"] = ("Sign in with ChatGPT", "Mit ChatGPT anmelden"),
            ["WeeklyRemainingQuota"] = ("WEEKLY REMAINING QUOTA", "WÖCHENTLICHES RESTKONTINGENT"),
            ["AiRiskNotice"] = ("AI RISK NOTICE", "KI-RISIKOHINWEIS"),
            ["ShowRiskNotice"] = ("Show risk notice", "Risikohinweis anzeigen"),
            ["RepositoryApproval"] = ("REPOSITORY APPROVAL", "REPOSITORY-FREIGABE"),
            ["RepositoryApprovalDescription"] = ("Approval applies only to the currently open repository.", "Die Freigabe gilt nur für das aktuell geöffnete Repository."),
            ["RevokeAiApproval"] = ("Revoke AI approval", "KI-Freigabe widerrufen"),
            ["Provider"] = ("PROVIDER", "ANBIETER"), ["BaseUrl"] = ("BASE URL", "BASIS-URL"),
            ["DetectionPattern"] = ("DETECTION PATTERN", "ERKENNUNGSMUSTER"),
            ["PatternDescription"] = ("Required for Custom; otherwise optional. A custom pattern must contain the (?<id>...) group.", "Bei Custom erforderlich, sonst optional. Ein eigenes Muster benötigt die Gruppe (?<id>...)."),
            ["Version"] = ("VERSION", "VERSION"), ["CheckForUpdates"] = ("Check for updates", "Nach Updates suchen"),
            ["ProjectOnGitHub"] = ("Project on GitHub", "Projekt auf GitHub"), ["Back"] = ("Back", "Zurück")
        };

    public string this[string key] =>
        Values.TryGetValue(key, out var value) ? language() == "Deutsch" ? value.De : value.En : key;
}
