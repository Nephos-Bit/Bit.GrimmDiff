namespace GrimmDiff.Logic.Abstracts;

public sealed record RepositorySnapshotModel(
    string Path,
    string Branch,
    RepositoryStateModel State,
    IReadOnlyList<GitFileChangeModel> Changes,
    IReadOnlyList<BranchInfoModel> Branches,
    IReadOnlyList<CommitInfoModel> History);