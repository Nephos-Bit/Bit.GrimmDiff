using GrimmDiff.Data.Adapters.Codex;
using GrimmDiff.Data.Adapters.Git;
using GrimmDiff.Data.Adapters.Preferences;
using GrimmDiff.Gui.Abstracts.Services;
using GrimmDiff.Gui.Services;
using GrimmDiff.Gui.ViewModels;
using GrimmDiff.Gui.Views;
using GrimmDiff.Logic.Abstracts.Ports;
using GrimmDiff.Logic.Processors.DiffAnalysis;
using GrimmDiff.Logic.UseCases;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace GrimmDiff.Gui.Composition;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _services;

    [STAThread]
    private static void Main()
    {
        VelopackApp.Build().SetAutoApplyOnStartup(false).Run();
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        var view = _services.GetRequiredService<MainView>();
        MainWindow = view;
        view.Closed += (_, _) => _services.Dispose();
        view.Show();
        Dispatcher.BeginInvoke(async () =>
        {
            var viewModel = _services.GetRequiredService<MainViewModel>();
            await viewModel.InitializeAsync();
            await viewModel.CheckForUpdatesOnStartupAsync();
        });
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<GitCliRepositoryCommandsAdapter>();
        services.AddSingleton<IRepositoryAvailabilityPort, GitCliRepositoryAvailabilityAdapter>();
        services.AddSingleton<IRepositoryReaderPort, GitCliRepositoryReaderAdapter>();
        services.AddSingleton<IRepositoryCommandsPort>(provider => provider.GetRequiredService<GitCliRepositoryCommandsAdapter>());
        services.AddSingleton<IRepositoryClonerPort>(provider => provider.GetRequiredService<GitCliRepositoryCommandsAdapter>());
        services.AddSingleton<IRepositoryDiffReaderPort, GitCliDiffReaderAdapter>();
        services.AddSingleton<ICodexSessionClientPort, CodexCliClientAdapter>();
        services.AddSingleton<ICodexStructuredRunnerPort, CodexCliStructuredRunnerAdapter>();
        services.AddSingleton<LocalHeuristicDiffAnalysisService>();
        services.AddSingleton<RepositoryWorkspaceService>();
        services.AddSingleton<DiffAnalysisService>(provider => new DiffAnalysisService(
            provider.GetRequiredService<IRepositoryDiffReaderPort>(),
            new CodexCliDiffAnalysisAdapter(provider.GetRequiredService<ICodexStructuredRunnerPort>(),
                provider.GetRequiredService<ICodexSessionClientPort>(),
                provider.GetRequiredService<LocalHeuristicDiffAnalysisService>()),
            provider.GetRequiredService<LocalHeuristicDiffAnalysisService>()));
        services.AddSingleton<CodexSessionService>();
        services.AddSingleton<PreferencesService>(provider =>
            new PreferencesService(new JsonAppPreferencesAdapter()));
        services.AddSingleton<IUserDialogService, WpfUserDialogService>();
        services.AddSingleton<IExternalNavigationService, ShellExternalNavigationService>();
        services.AddSingleton<IRepositoryChangeMonitorService, FileSystemRepositoryChangeMonitorService>();
        services.AddSingleton<IApplicationUpdateService, VelopackUpdateService>();
        services.AddSingleton<WorkspaceStatusViewModel>();
        services.AddSingleton<PreferencesStateService>();
        services.AddSingleton<AiConsentService>();
        services.AddSingleton<ApplicationUpdateCoordinator>();
        services.AddSingleton<RepositoryWorkspaceViewModel>();
        services.AddSingleton<DiffDocumentViewModel>();
        services.AddSingleton<CodexAccountViewModel>();
        services.AddSingleton<AnalysisWorkspaceViewModel>();
        services.AddSingleton<WorkspaceViewModel>();
        services.AddSingleton<AppearanceSettingsViewModel>();
        services.AddSingleton<MascotSettingsViewModel>();
        services.AddSingleton<AiAccountSettingsViewModel>();
        services.AddSingleton<AiPrivacySettingsViewModel>();
        services.AddSingleton<AiPromptSettingsViewModel>();
        services.AddSingleton<TicketLinksSettingsViewModel>();
        services.AddSingleton<AboutSettingsViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<MainView>();
    }
}
