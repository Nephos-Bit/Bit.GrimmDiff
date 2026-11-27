using System.Globalization;
using GrimmDiff.Logic.Abstracts.Models;

namespace GrimmDiff.Data.Services;

internal static class GitCliSnapshotParser
{
    public static IReadOnlyList<GitFileChangeModel> ParseChanges(string text)
    {
        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line =>
        {
            var index = Kind(line[0]);
            var worktree = Kind(line[1]);
            var path = line[3..];
            return new[]
            {
                index == null ? null : new GitFileChangeModel(path, index.Value, DiffScopeModel.Staged),
                worktree == null ? null : new GitFileChangeModel(path, worktree.Value, DiffScopeModel.WorkingTree)
            };
        }).SelectMany(x => x).OfType<GitFileChangeModel>().ToList();
    }

    public static IReadOnlyList<BranchInfoModel> ParseBranches(string text)
    {
        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('|'))
            .Where(x => x.Length == 2).Select(x => new BranchInfoModel(x[1], x[0] == "*")).ToList();
    }

    public static IReadOnlyList<CommitInfoModel> ParseHistory(string text)
    {
        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('\x1f'))
            .Where(x => x.Length == 4).Select(x =>
                new CommitInfoModel(x[0], x[1], x[2], DateTimeOffset.Parse(x[3], CultureInfo.InvariantCulture))).ToList();
    }

    private static ChangeKindModel? Kind(char value)
    {
        return value switch
        {
            ' ' => null, 'M' => ChangeKindModel.Modified, 'A' => ChangeKindModel.Added, 'D' => ChangeKindModel.Deleted,
            'R' => ChangeKindModel.Renamed, '?' => ChangeKindModel.Untracked, 'U' => ChangeKindModel.Conflicted,
            _ => ChangeKindModel.Modified
        };
    }
}
