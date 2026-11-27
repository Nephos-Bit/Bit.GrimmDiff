namespace GrimmDiff.Logic.Abstracts.Models;

public sealed record AiAnalysisPreviewModel(
    int TotalSections,
    int TransferableSections,
    int ExcludedSections,
    int TransferableCharacters);