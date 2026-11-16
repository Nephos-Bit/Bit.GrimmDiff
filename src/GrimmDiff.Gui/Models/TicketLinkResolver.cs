using System.Text.RegularExpressions;
using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Gui;

public static partial class TicketLinkResolver
{
    public static IReadOnlyList<CommitSubjectPartModel> Split(string subject, TicketLinkSettingsModel settings)
    {
        if (!TryGetBaseUri(settings, out var baseUri) || settings.Provider == TicketLinkProviderModel.Disabled)
            return [new PlainCommitSubjectPartModel(subject)];

        var matches = Pattern(settings.Provider).Matches(subject);
        if (matches.Count == 0) return [new PlainCommitSubjectPartModel(subject)];

        var parts = new List<CommitSubjectPartModel>();
        var index = 0;
        foreach (Match match in matches)
        {
            if (match.Index > index) parts.Add(new PlainCommitSubjectPartModel(subject[index..match.Index]));
            parts.Add(new TicketCommitSubjectPartModel(match.Value, BuildUrl(baseUri, settings.Provider, match)));
            index = match.Index + match.Length;
        }

        if (index < subject.Length) parts.Add(new PlainCommitSubjectPartModel(subject[index..]));
        return parts;
    }

    public static bool IsValidBaseUrl(TicketLinkProviderModel provider, string? value)
    {
        if (provider == TicketLinkProviderModel.Disabled || !Uri.TryCreate(value, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo) ||
            !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return false;
        return provider switch
        {
            TicketLinkProviderModel.Jira => uri.Host.EndsWith(".atlassian.net", StringComparison.OrdinalIgnoreCase),
            TicketLinkProviderModel.GitHub => uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
                                                HasPathSegments(uri, 2, 2),
            TicketLinkProviderModel.GitLab => uri.Host.Equals("gitlab.com", StringComparison.OrdinalIgnoreCase) &&
                                               HasPathSegments(uri, 2, int.MaxValue),
            TicketLinkProviderModel.AzureDevOps => uri.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase) &&
                                                     HasPathSegments(uri, 2, 2),
            _ => false
        };
    }

    private static string BuildUrl(Uri baseUri, TicketLinkProviderModel provider, Match match)
    {
        var baseUrl = baseUri.AbsoluteUri.TrimEnd('/');
        return provider switch
        {
            TicketLinkProviderModel.Jira => $"{baseUrl}/browse/{Uri.EscapeDataString(match.Groups["key"].Value)}",
            TicketLinkProviderModel.GitHub => $"{baseUrl}/issues/{match.Groups["number"].Value}",
            TicketLinkProviderModel.GitLab => $"{baseUrl}/-/issues/{match.Groups["number"].Value}",
            TicketLinkProviderModel.AzureDevOps => $"{baseUrl}/_workitems/edit/{match.Groups["number"].Value}",
            _ => throw new InvalidOperationException("A disabled ticket provider cannot create URLs.")
        };
    }

    private static Regex Pattern(TicketLinkProviderModel provider)
    {
        return provider switch
        {
            TicketLinkProviderModel.Jira => JiraPattern(),
            TicketLinkProviderModel.GitHub or TicketLinkProviderModel.GitLab => NumberPattern(),
            TicketLinkProviderModel.AzureDevOps => AzureDevOpsPattern(),
            _ => EmptyPattern()
        };
    }

    private static bool TryGetBaseUri(TicketLinkSettingsModel settings, out Uri baseUri)
    {
        if (settings.Provider != TicketLinkProviderModel.Disabled && IsValidBaseUrl(settings.Provider, settings.BaseUrl) &&
            Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out baseUri!)) return true;
        baseUri = null!;
        return false;
    }

    [GeneratedRegex("(?<![A-Za-z0-9])(?<key>[A-Za-z][A-Za-z0-9]*-\\d+)(?![A-Za-z0-9])")]
    private static partial Regex JiraPattern();

    [GeneratedRegex("(?<![A-Za-z0-9_#])#(?<number>\\d+)\\b")]
    private static partial Regex NumberPattern();

    [GeneratedRegex("(?<![A-Za-z0-9])AB#(?<number>\\d+)\\b", RegexOptions.IgnoreCase)]
    private static partial Regex AzureDevOpsPattern();

    [GeneratedRegex("(?!)")]
    private static partial Regex EmptyPattern();

    private static bool HasPathSegments(Uri uri, int minimum, int maximum)
    {
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= minimum && segments.Length <= maximum;
    }
}
