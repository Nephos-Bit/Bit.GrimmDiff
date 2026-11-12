namespace GrimmDiff.Domain;

public static class AiDiffSafetyPolicy
{
    public const string Version = "2026-11-v1";
    public const int MaximumRemoteHunkCharacters = 64_000;
    public const int MaximumRemoteCharacters = 240_000;

    private static readonly string[] ProtectedNames =
    [
        ".env", ".npmrc", ".netrc", ".pypirc", "credentials", "credentials.json", "secrets.json", "secret.json",
        "id_rsa", "id_ed25519", "known_hosts", "authorized_keys", "wallet.dat"
    ];

    private static readonly string[] ProtectedExtensions =
        [".pem", ".key", ".pfx", ".p12", ".kdbx", ".jks", ".keystore", ".cer", ".crt", ".der", ".ovpn"];

    private static readonly string[] ProtectedDirectories = [".aws", ".ssh", ".gnupg", ".azure", ".kube"];

    public static AiDiffSafetyAssessment Assess(string diff, DiffScope scope)
    {
        var transferable = new List<DiffHunk>();
        var excluded = new List<DiffHunk>();
        var transferredCharacters = 0;
        foreach (var hunk in UnifiedDiffParser.Parse(diff, scope))
        {
            var characterCount = hunk.Header.Length + hunk.Content.Length;
            if (IsProtected(hunk) || characterCount > MaximumRemoteHunkCharacters ||
                transferredCharacters + characterCount > MaximumRemoteCharacters)
            {
                excluded.Add(hunk);
                continue;
            }

            transferable.Add(hunk);
            transferredCharacters += characterCount;
        }

        return new AiDiffSafetyAssessment(transferable, excluded,
            new AiAnalysisPreview(transferable.Count + excluded.Count, transferable.Count, excluded.Count,
                transferredCharacters));
    }

    private static bool IsProtected(DiffHunk hunk)
    {
        var normalized = hunk.FilePath.Replace('\\', '/');
        var name = Path.GetFileName(normalized);
        return hunk.Content.Contains("Binary files", StringComparison.OrdinalIgnoreCase) ||
               hunk.Content.Contains("GIT binary patch", StringComparison.OrdinalIgnoreCase) ||
               ProtectedNames.Any(value =>
                   name.Equals(value, StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith(value + ".", StringComparison.OrdinalIgnoreCase)) ||
               ProtectedExtensions.Any(extension =>
                   normalized.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) ||
               normalized.Split('/').Any(segment =>
                   ProtectedDirectories.Contains(segment, StringComparer.OrdinalIgnoreCase));
    }
}