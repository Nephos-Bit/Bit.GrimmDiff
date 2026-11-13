namespace GrimmDiff.Gui.Abstracts;

public sealed record ApplicationUpdateCheckResultModel(ApplicationUpdateStatusModel Status, string? Version = null);