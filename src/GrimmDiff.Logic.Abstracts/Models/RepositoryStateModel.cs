namespace GrimmDiff.Logic.Abstracts;

public enum RepositoryStateModel
{
    Ready,
    Conflict,
    MergeInProgress,
    RebaseInProgress,
    CherryPickInProgress,
    Unavailable
}