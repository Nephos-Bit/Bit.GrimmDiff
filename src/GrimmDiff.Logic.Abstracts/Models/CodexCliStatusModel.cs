namespace GrimmDiff.Logic.Abstracts.Models;

public sealed record CodexCliStatusModel(CodexCliStatusKindModel Kind, string Message)
{
    public bool IsReady => Kind == CodexCliStatusKindModel.Ready;
}