using System.Text.RegularExpressions;
using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Gui;

public sealed class CommitHistoryItemModel
{
    private static readonly Regex JiraIssuePrefix =
        new(@"^\[(?<issue>[A-Za-z][A-Za-z0-9]*-\d+)\]", RegexOptions.Compiled);

    public CommitHistoryItemModel(CommitInfoModel commit)
    {
        Commit = commit;
        var match = JiraIssuePrefix.Match(commit.Subject);
        SubjectParts = match.Success
            ?
            [
                new JiraCommitSubjectPartModel(match.Value, $"https://jira/browse/{match.Groups["issue"].Value}"),
                new PlainCommitSubjectPartModel(commit.Subject[match.Length..])
            ]
            : [new PlainCommitSubjectPartModel(commit.Subject)];
    }

    public CommitInfoModel Commit { get; }
    public IReadOnlyList<CommitSubjectPartModel> SubjectParts { get; }
}