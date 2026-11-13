namespace GrimmDiff.Logic.Abstracts;

public sealed record CodexCliStatusModel(CodexCliStatusKindModel Kind, string Message)
{
    public bool IsReady => Kind == CodexCliStatusKindModel.Ready;
}