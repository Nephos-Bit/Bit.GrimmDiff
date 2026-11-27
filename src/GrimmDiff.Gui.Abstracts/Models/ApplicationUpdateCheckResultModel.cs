namespace GrimmDiff.Gui.Abstracts.Models;

public sealed record ApplicationUpdateCheckResultModel(ApplicationUpdateStatusModel Status, string? Version = null);