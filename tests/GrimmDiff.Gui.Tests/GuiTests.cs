using GrimmDiff.Gui;

var language = "English";
var text = new LocalizedTextService(() => language);

if (text["Appearance"] != "Appearance" || text["RepositoryApprovalDescription"] != "Approval applies only to the currently open repository.")
    throw new Exception("Expected English settings text.");

language = "Deutsch";
if (text["Appearance"] != "Darstellung" || text["RepositoryApprovalDescription"] != "Die Freigabe gilt nur für das aktuell geöffnete Repository.")
    throw new Exception("Expected German settings text.");

Console.WriteLine("PASS Gui project structure");
Console.WriteLine("PASS settings text switches language");
