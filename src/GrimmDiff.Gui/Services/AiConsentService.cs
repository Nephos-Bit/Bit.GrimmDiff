using System.IO;
using System.Security.Cryptography;
using GrimmDiff.Gui.Abstracts.Services;
using GrimmDiff.Gui.ViewModels;
using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.Abstracts.Policies;

namespace GrimmDiff.Gui.Services;

public sealed class AiConsentService(PreferencesStateService preferences, IUserDialogService dialogs,
    WorkspaceStatusViewModel status)
{
    public bool ShowRiskNotice()
    {
        var german = preferences.Language == "Deutsch";
        var confirmation = dialogs.Ask(german ? "KI-Risikohinweis" : "AI risk notice", german
            ? "Die Anwendung hat Zugriff auf das von dir geöffnete lokale Git-Repository. Die optionale KI-Analyse benötigt eine separate Repository-Freigabe. Vor jeder Analyse zeigt die Anwendung Umfang und ausgeschlossene Abschnitte; sensible, binäre und zu große Änderungen bleiben lokal. Die KI wird im angemeldeten ChatGPT-Workspace ausgeführt; sie hat keinen freien Lesezugriff auf weitere Repository-Dateien.\n\nTippe ACCEPT, um die Anwendung zu verwenden."
            : "The application can access the local Git repository you open. Optional AI analysis requires separate repository approval. Before every analysis, the application shows the scope and excluded sections; sensitive, binary, and oversized changes stay local. AI runs in your signed-in ChatGPT workspace and has no unrestricted access to other repository files.\n\nType ACCEPT to use the application.");
        if (!string.Equals(confirmation, "ACCEPT", StringComparison.Ordinal)) return false;
        preferences.SetAiRiskNoticeAccepted();
        return true;
    }

    public bool Approve(string repositoryPath, AiAnalysisPreviewModel preview)
    {
        var repositoryId = RepositoryIdentity(repositoryPath);
        if (preferences.Approvals.Any(approval => approval.RepositoryId == repositoryId &&
                                                  approval.ProviderId == "ChatGPT Codex CLI" &&
                                                  approval.SafetyPolicyVersion == AiDiffSafetyPolicy.Version)) return true;

        var confirmation = dialogs.Ask("AI analysis",
            $"This repository approval permits optional processing by the signed-in ChatGPT workspace. This analysis will transfer {preview.TransferableSections} changed section(s), up to {preview.TransferableCharacters:N0} characters. {preview.ExcludedSections} section(s) are excluded and stay local. Approval is invalidated when repository configuration, provider, or the safety policy changes. Type ALLOW to approve this repository.");
        if (!string.Equals(confirmation, "ALLOW", StringComparison.Ordinal))
        {
            status.Message = "AI analysis was not approved.";
            return false;
        }

        var root = Path.GetFullPath(repositoryPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var approvals = preferences.Approvals.Where(approval =>
            !approval.RepositoryId.StartsWith(root + "|", StringComparison.OrdinalIgnoreCase)).ToList();
        approvals.Add(new AiRepositoryApprovalModel(repositoryId, "ChatGPT Codex CLI",
            AiDiffSafetyPolicy.Version, DateTimeOffset.UtcNow));
        preferences.SetApprovals(approvals);
        return true;
    }

    public void Revoke(string repositoryPath)
    {
        var root = Path.GetFullPath(repositoryPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        preferences.SetApprovals(preferences.Approvals.Where(approval =>
            !approval.RepositoryId.StartsWith(root + "|", StringComparison.OrdinalIgnoreCase)).ToList());
        status.Message = preferences.Language == "Deutsch"
            ? "Die KI-Freigabe für dieses Repository wurde widerrufen."
            : "AI approval for this repository was revoked.";
    }

    private static string RepositoryIdentity(string repositoryPath)
    {
        var root = Path.GetFullPath(repositoryPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var config = Path.Combine(root, ".git", "config");
        var configHash = File.Exists(config)
            ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(config)))
            : "NO_CONFIG";
        return $"{root}|{configHash}";
    }
}
