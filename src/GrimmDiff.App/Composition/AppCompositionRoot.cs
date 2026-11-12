using GrimmDiff.Application;
using GrimmDiff.Infrastructure;

namespace GrimmDiff.App;

public static class AppCompositionRoot
{
    public static MainViewModel CreateMainViewModel()
    {
        var git = new GitCliRepositoryService();
        var commands = new GitCliRepositoryCommands(git);
        var codex = new CodexCliClient();
        var heuristic = new LocalHeuristicDiffAnalysisProvider();
        return new MainViewModel(
            new RepositoryWorkspaceService(new GitCliRepositoryAvailability(git), new GitCliRepositoryReader(git),
                commands, commands),
            new DiffAnalysisService(new GitCliDiffReader(git), new CodexCliDiffAnalysisProvider(codex, heuristic),
                heuristic),
            new CodexSessionService(codex),
            new PreferencesService(new AppSettingsStore()),
            new WpfUserDialogService(),
            new ShellExternalNavigationService(),
            new FileSystemRepositoryChangeMonitor(),
            new VelopackUpdateService());
    }
}
