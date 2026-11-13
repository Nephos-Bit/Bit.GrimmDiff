namespace GrimmDiff.Logic.Abstracts;

public sealed record AiAnalysisPreviewModel(
    int TotalSections,
    int TransferableSections,
    int ExcludedSections,
    int TransferableCharacters);