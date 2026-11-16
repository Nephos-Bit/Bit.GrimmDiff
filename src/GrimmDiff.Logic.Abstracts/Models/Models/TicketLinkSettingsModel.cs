namespace GrimmDiff.Logic.Abstracts;

public sealed record TicketLinkSettingsModel(TicketLinkProviderModel Provider, string? BaseUrl, string? Pattern = null)
{
    public static TicketLinkSettingsModel Disabled { get; } = new(TicketLinkProviderModel.Disabled, null);
}
