namespace GrimmDiff.Domain;

public sealed record RepositorySnapshot(
    string Path,
    string Branch,
    RepositoryState State,
    IReadOnlyList<GitFileChange> Changes,
    IReadOnlyList<BranchInfo> Branches,
    IReadOnlyList<CommitInfo> History);