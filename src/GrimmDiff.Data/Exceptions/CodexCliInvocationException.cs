using System.ComponentModel;
using System.Diagnostics;
using GrimmDiff.Data.Abstracts;

namespace GrimmDiff.Data;

public sealed class CodexCliInvocationException(string message, string diagnostic) : Exception(message)
{
    public string Diagnostic { get; } = diagnostic;
}
