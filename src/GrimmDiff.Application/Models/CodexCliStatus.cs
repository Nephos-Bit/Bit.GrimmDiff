namespace GrimmDiff.Application;

public sealed record CodexCliStatus(CodexCliStatusKind Kind, string Message)
{
    public bool IsReady => Kind == CodexCliStatusKind.Ready;
}