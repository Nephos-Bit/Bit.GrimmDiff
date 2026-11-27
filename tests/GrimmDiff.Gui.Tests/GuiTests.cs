using GrimmDiff.Gui;
using System.Threading;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Windows.Data;
using GrimmDiff.Gui.Composition;
using GrimmDiff.Gui.Controls;
using GrimmDiff.Gui.Models;
using GrimmDiff.Gui.Services;
using GrimmDiff.Gui.Tests;
using GrimmDiff.Gui.ViewModels;
using GrimmDiff.Gui.Views;
using GrimmDiff.Gui.Views.Settings;
using GrimmDiff.Logic;
using GrimmDiff.Logic.Abstracts;
using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.UseCases;

var language = "English";
var text = new LocalizedTextService(() => language);

if (text["Appearance"] != "Appearance" || text["RepositoryApprovalDescription"] != "Approval applies only to the currently open repository.")
    throw new Exception("Expected English settings text.");

language = "Deutsch";
if (text["Appearance"] != "Darstellung" || text["RepositoryApprovalDescription"] != "Die Freigabe gilt nur für das aktuell geöffnete Repository." ||
    text["ChooseMascot"] != "MASKOTTCHEN W\u00C4HLEN")
    throw new Exception("Expected German settings text.");

Console.WriteLine("PASS Gui project structure");
Console.WriteLine("PASS settings text switches language");

var mascots = MascotCatalog.Options;
if (mascots.Count != 8 || mascots.Select(mascot => mascot.Id).Distinct().Count() != 8 ||
    mascots.Select(mascot => mascot.Name).Distinct().Count() != 8 ||
    mascots.Any(mascot => mascot.Outline is null || mascot.Body is null || mascot.Accent is null || mascot.Highlight is null) ||
    MascotCatalog.Find("sgt-schnabel").Name != "Sgt. Schnabel" ||
    MascotCatalog.Find("berta").Name != "Berta" ||
    MascotCatalog.Find("missing").Id != MascotCatalog.DefaultId)
    throw new Exception("Expected eight named, selectable mascots and a safe fallback.");

Console.WriteLine("PASS mascot options");

Exception? renderingError = null;
var renderThread = new Thread(() =>
{
    try
    {
        var metadata = (FrameworkPropertyMetadata)ReviewMascotControl.VariantProperty.GetMetadata(typeof(ReviewMascotControl));
        if (!metadata.AffectsRender)
            throw new Exception("Changing a mascot variant must invalidate its visual.");

        var control = new ReviewMascotControl();
        control.Measure(new Size(36, 36));
        control.Arrange(new Rect(0, 0, 36, 36));

        byte[] Snapshot()
        {
            var image = new RenderTargetBitmap(36, 36, 96, 96, PixelFormats.Pbgra32);
            image.Render(control);
            var pixels = new byte[36 * 36 * 4];
            image.CopyPixels(pixels, 36 * 4, 0);
            return pixels;
        }

        bool ContainsColor(byte[] pixels, string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            for (var index = 0; index < pixels.Length; index += 4)
            {
                if (pixels[index] == color.B && pixels[index + 1] == color.G &&
                    pixels[index + 2] == color.R && pixels[index + 3] == color.A)
                    return true;
            }
            return false;
        }

        void FlushRender()
        {
            var frame = new DispatcherFrame();
            control.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        var snapshots = new List<byte[]> { Snapshot() };
        foreach (var option in MascotCatalog.Options.Skip(1))
        {
            control.Variant = option.Id;
            FlushRender();
            var changed = Snapshot();
            if (snapshots.Any(previous => previous.SequenceEqual(changed)))
                throw new Exception($"Mascot {option.Name} did not produce a distinct rendered preview.");
            if (option.Id == "sgt-schnabel" &&
                (!ContainsColor(changed, "#F4CA3D") || !ContainsColor(changed, "#E98226")))
                throw new Exception("Sgt. Schnabel must render a yellow body and orange bill.");
            if (option.Id == "berta" &&
                (!ContainsColor(changed, "#FFFDF3") || !ContainsColor(changed, "#D93B2A")))
                throw new Exception("Berta must render a white body and red comb.");
            snapshots.Add(changed);
        }

        control.SetValue(ReviewMascotControl.OutlineBrushProperty, null);
        FlushRender();
        if (Snapshot().All(value => value == 0))
            throw new Exception("Variant palettes should not depend on the default mascot brushes.");

        control.Variant = MascotCatalog.DefaultId;
        FlushRender();
        if (Snapshot().Any(value => value != 0))
            throw new Exception("A mascot with a missing selected brush should not be rendered.");

        control.ClearValue(ReviewMascotControl.OutlineBrushProperty);
        control.Variant = "wisp";
        FlushRender();
        control.Variant = null;
        FlushRender();
        if (!snapshots[0].SequenceEqual(Snapshot()))
            throw new Exception("A null mascot variant should render the default mascot.");
    }
    catch (Exception exception)
    {
        renderingError = exception;
    }
});
renderThread.SetApartmentState(ApartmentState.STA);
renderThread.Start();
renderThread.Join();
if (renderingError is not null) throw renderingError;

Console.WriteLine("PASS mascot preview redraws after variant changes");

Exception? compositionError = null;
var compositionThread = new Thread(() =>
{
    try
    {
        var app = new App();
        app.InitializeComponent();
        var services = new ServiceCollection();
        var configure = typeof(App).GetMethod("ConfigureServices", BindingFlags.Static | BindingFlags.NonPublic)
                        ?? throw new Exception("Application service composition was not found.");
        configure.Invoke(null, [services]);
        using var provider = services.BuildServiceProvider();
        var view = provider.GetRequiredService<MainView>();
        var main = provider.GetRequiredService<MainViewModel>();
        view.ShowActivated = false;
        view.ShowInTaskbar = false;
        view.Opacity = 0;
        view.Show();
        view.Measure(new Size(1440, 900));
        view.Arrange(new Rect(0, 0, 1440, 900));
        view.UpdateLayout();

        T? Find<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent is T match) return match;
            for (var index = 0; index < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var found = Find<T>(System.Windows.Media.VisualTreeHelper.GetChild(parent, index));
                if (found is not null) return found;
            }
            return null;
        }

        string? ContextPath(FrameworkElement? element) => element is null ? null :
            BindingOperations.GetBindingExpression(element, FrameworkElement.DataContextProperty)?.ParentBinding.Path?.Path;
        var content = (DependencyObject)view.Content;
        if (view.DataContext != main ||
            ContextPath(Find<WorkspaceView>(content)) != nameof(MainViewModel.Workspace) ||
            ContextPath(Find<RepositoryView>(content)) != nameof(WorkspaceViewModel.Repository) ||
            ContextPath(Find<AnalysisView>(content)) != nameof(WorkspaceViewModel.Analysis) ||
            ContextPath(Find<DiffDocumentView>(content)) != nameof(WorkspaceViewModel.Diff) ||
            ContextPath(Find<CodexAccountView>(content)) != nameof(MainViewModel.Account) ||
            ContextPath(Find<SettingsView>(content)) != nameof(MainViewModel.Settings) ||
            Find<WorkspaceView>(content)?.DataContext != main.Workspace ||
            Find<RepositoryView>(content)?.DataContext != main.Workspace.Repository ||
            Find<AnalysisView>(content)?.DataContext != main.Workspace.Analysis ||
            Find<DiffDocumentView>(content)?.DataContext != main.Workspace.Diff ||
            Find<CodexAccountView>(content)?.DataContext != main.Account ||
            Find<SettingsView>(content)?.DataContext != main.Settings)
            throw new Exception("Each view must bind to its own view model.");
        main.OpenSettingsCommand.Execute(null);
        view.UpdateLayout();
        if (!main.IsSettingsOpen || Find<SettingsView>(content)?.Visibility != Visibility.Visible ||
            Find<WorkspaceView>(content)?.Visibility != Visibility.Collapsed)
            throw new Exception("Settings navigation must show the settings view.");
        main.Settings.BackToWorkspaceCommand.Execute(null);
        view.UpdateLayout();
        if (main.IsSettingsOpen || Find<SettingsView>(content)?.Visibility != Visibility.Collapsed ||
            Find<WorkspaceView>(content)?.Visibility != Visibility.Visible)
            throw new Exception("Back navigation must restore the workspace view.");
        var pendingAnalysis = new TaskCompletionSource();
        main.Workspace.Repository.ContextChanged += () => pendingAnalysis.Task;
        var currentChanges = main.Workspace.Repository.ShowCurrentCommand;
        currentChanges.Execute(null);
        if (currentChanges.CanExecute(null))
            throw new Exception("Current changes must be disabled while its analysis is loading.");
        pendingAnalysis.SetResult();
        var completionFrame = new DispatcherFrame();
        view.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => completionFrame.Continue = false));
        Dispatcher.PushFrame(completionFrame);
        if (!currentChanges.CanExecute(null))
            throw new Exception("Current changes must be enabled after its analysis finishes.");
        view.Close();
        app.Shutdown();
    }
    catch (Exception exception)
    {
        compositionError = exception;
    }
});
compositionThread.SetApartmentState(ApartmentState.STA);
compositionThread.Start();
compositionThread.Join();
if (compositionError is not null) throw compositionError;
Console.WriteLine("PASS workspace views bind to their own view models");

var preferenceStore = new InMemoryPreferencesStore(new AppPreferencesModel([], Font: "Unknown"));
var preferences = new PreferencesStateService(new PreferencesService(preferenceStore));
await preferences.LoadAsync();
if (preferences.Font != "Cascadia Code") throw new Exception("Legacy font values must use the supported default.");
preferences.SetLanguage("Deutsch");
preferences.SetTheme("Dark");
preferences.AddRecent("repository-a");
await preferences.FlushAsync();
if (preferenceStore.Writes.Count != 3 || preferenceStore.Current.Language != "Deutsch" ||
    preferenceStore.Current.Theme != "Dark" || preferenceStore.Current.RecentRepositories.Single() != "repository-a" ||
    preferences.Text["Appearance"] != "Darstellung")
    throw new Exception("Preference changes must be saved in order and reflected in localized settings.");
Console.WriteLine("PASS preferences load legacy values and serialize updates");

var dialogs = new RecordingDialogService();
dialogs.Answers.Enqueue("ALLOW");
dialogs.Answers.Enqueue("ALLOW");
var consent = new AiConsentService(preferences, dialogs, new WorkspaceStatusViewModel());
var repositoryRoot = Path.Combine(Path.GetTempPath(), "GrimmDiff-consent-" + Guid.NewGuid().ToString("N"));
var firstRepository = Path.Combine(repositoryRoot, "repo");
var siblingRepository = Path.Combine(repositoryRoot, "repo-other");
var preview = new AiAnalysisPreviewModel(2, 1, 1, 40);
if (!consent.Approve(firstRepository, preview) || !consent.Approve(firstRepository, preview) ||
    !consent.Approve(siblingRepository, preview) || dialogs.Questions != 2)
    throw new Exception("Repository approval must be reused only for the same repository and policy.");
consent.Revoke(firstRepository);
await preferences.FlushAsync();
if (preferences.Approvals.Count != 1 || !preferences.Approvals[0].RepositoryId.StartsWith(siblingRepository,
        StringComparison.OrdinalIgnoreCase))
    throw new Exception("Revoking a repository must preserve approvals for sibling paths.");
Console.WriteLine("PASS AI approval reuse and repository-scoped revocation");
