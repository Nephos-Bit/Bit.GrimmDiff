namespace GrimmDiff.Data.Exceptions;

public sealed class CodexCliInvocationException(string message, string diagnostic) : Exception(message)
{
    public string Diagnostic { get; } = diagnostic;
}
