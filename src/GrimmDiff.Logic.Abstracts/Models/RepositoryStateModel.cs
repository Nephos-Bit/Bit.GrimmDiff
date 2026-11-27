namespace GrimmDiff.Logic.Abstracts.Models;

public enum RepositoryStateModel
{
    Ready,
    Conflict,
    MergeInProgress,
    RebaseInProgress,
    CherryPickInProgress,
    Unavailable
}