namespace GrimmDiff.Domain;

public sealed record AiAnalysisPreview(int TotalSections, int TransferableSections, int ExcludedSections,
    int TransferableCharacters);
