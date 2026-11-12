using System.Text.RegularExpressions;
using GrimmDiff.Domain;

namespace GrimmDiff.App;

public sealed class CommitHistoryItem
{
    private static readonly Regex JiraIssuePrefix =
        new(@"^\[(?<issue>[A-Za-z][A-Za-z0-9]*-\d+)\]", RegexOptions.Compiled);

    public CommitHistoryItem(CommitInfo commit)
    {
        Commit = commit;
        var match = JiraIssuePrefix.Match(commit.Subject);
        SubjectParts = match.Success
            ?
            [
                new JiraCommitSubjectPart(match.Value, $"https://jira/browse/{match.Groups["issue"].Value}"),
                new PlainCommitSubjectPart(commit.Subject[match.Length..])
            ]
            : [new PlainCommitSubjectPart(commit.Subject)];
    }

    public CommitInfo Commit { get; }
    public IReadOnlyList<CommitSubjectPart> SubjectParts { get; }
}