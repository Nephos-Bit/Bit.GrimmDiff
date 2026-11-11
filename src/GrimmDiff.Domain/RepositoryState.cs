namespace GrimmDiff.Domain;

public enum RepositoryState
{
    Ready, Conflict, MergeInProgress, RebaseInProgress, CherryPickInProgress, Unavailable
}
