using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GrimmDiff.Gui.Models;

namespace GrimmDiff.Gui.Controls;

public sealed class ReviewMascotControl : FrameworkElement
{
    private readonly DispatcherTimer _timer;
    private Window? _window;
    private int _frame;
    private int _gazeX;
    private int _gazeY;

    private bool CanAnimate => IsLoaded && IsVisible &&
                               _window is { WindowState: not WindowState.Minimized } &&
                               SystemParameters.ClientAreaAnimation;

    public ReviewMascotControl()
    {
        Width = 36;
        Height = 36;
        IsHitTestVisible = false;
        SnapsToDevicePixels = true;
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _timer.Tick += OnTick;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsVisibleChanged += OnIsVisibleChanged;
    }

    public static readonly DependencyProperty IsAnalyzingProperty = DependencyProperty.Register(
        nameof(IsAnalyzing), typeof(bool), typeof(ReviewMascotControl),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnIsAnalyzingChanged));

    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(string), typeof(ReviewMascotControl),
        new FrameworkPropertyMetadata(MascotCatalog.DefaultId, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OutlineBrushProperty = RegisterBrush(nameof(OutlineBrush));
    public static readonly DependencyProperty BodyBrushProperty = RegisterBrush(nameof(BodyBrush));
    public static readonly DependencyProperty AccentBrushProperty = RegisterBrush(nameof(AccentBrush));
    public static readonly DependencyProperty HighlightBrushProperty = RegisterBrush(nameof(HighlightBrush));

    public bool IsAnalyzing
    {
        get => (bool)GetValue(IsAnalyzingProperty);
        set => SetValue(IsAnalyzingProperty, value);
    }

    public string? Variant
    {
        get => (string?)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public Brush OutlineBrush
    {
        get => (Brush)GetValue(OutlineBrushProperty);
        set => SetValue(OutlineBrushProperty, value);
    }

    public Brush BodyBrush
    {
        get => (Brush)GetValue(BodyBrushProperty);
        set => SetValue(BodyBrushProperty, value);
    }

    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public Brush HighlightBrush
    {
        get => (Brush)GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        var option = MascotCatalog.Find(Variant);
        var outline = option.Id == MascotCatalog.DefaultId ? OutlineBrush : option.Outline;
        var body = option.Id == MascotCatalog.DefaultId ? BodyBrush : option.Body;
        var accent = option.Id == MascotCatalog.DefaultId ? AccentBrush : option.Accent;
        var highlight = option.Id == MascotCatalog.DefaultId ? HighlightBrush : option.Highlight;
        if (outline is null || body is null || accent is null || highlight is null) return;
        MascotSpriteRenderer.Render(context, option, outline, body, accent, highlight,
            CanAnimate, IsAnalyzing, _frame, _gazeX, _gazeY);
    }
    private static DependencyProperty RegisterBrush(string name)
    {
        var defaultBrush = name switch
        {
            nameof(OutlineBrush) => MascotCatalog.Options[0].Outline,
            nameof(BodyBrush) => MascotCatalog.Options[0].Body,
            nameof(AccentBrush) => MascotCatalog.Options[0].Accent,
            _ => MascotCatalog.Options[0].Highlight
        };
        return DependencyProperty.Register(name, typeof(Brush), typeof(ReviewMascotControl),
            new FrameworkPropertyMetadata(defaultBrush, FrameworkPropertyMetadataOptions.AffectsRender));
    }

    private static void OnIsAnalyzingChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var mascot = (ReviewMascotControl)dependencyObject;
        mascot._frame = 0;
        mascot._timer.Interval = TimeSpan.FromMilliseconds(mascot.IsAnalyzing ? 125 : 500);
        mascot.InvalidateVisual();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachToWindow(Window.GetWindow(this));
        SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
        UpdateAnimation();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        AttachToWindow(null);
        SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
        UpdateAnimation();
    }

    private void AttachToWindow(Window? window)
    {
        if (ReferenceEquals(_window, window)) return;

        if (_window is not null)
        {
            _window.PreviewMouseMove -= OnWindowMouseMove;
            _window.MouseLeave -= OnWindowMouseLeave;
            _window.StateChanged -= OnWindowStateChanged;
        }

        _window = window;
        if (_window is not null)
        {
            _window.PreviewMouseMove += OnWindowMouseMove;
            _window.MouseLeave += OnWindowMouseLeave;
            _window.StateChanged += OnWindowStateChanged;
        }

        _gazeX = 0;
        _gazeY = 0;
        InvalidateVisual();
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateAnimation();
    }

    private void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.PropertyName) && e.PropertyName != nameof(SystemParameters.ClientAreaAnimation))
            return;
        UpdateAnimation();
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (CanAnimate)
        {
            _timer.Start();
        }
        else
        {
            _timer.Stop();
            _frame = 0;
            _gazeX = 0;
            _gazeY = 0;
            InvalidateVisual();
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (!CanAnimate) return;
        _frame = (_frame + 1) % 84;
        InvalidateVisual();
    }

    private void OnWindowMouseMove(object sender, MouseEventArgs e)
    {
        if (!ReferenceEquals(sender, _window) || !CanAnimate) return;
        var point = e.GetPosition(this);
        var x = point.X - ActualWidth / 2;
        var y = point.Y - ActualHeight / 2;
        var gazeX = x < -12 ? -1 : x > 12 ? 1 : 0;
        var gazeY = y < -12 ? -1 : y > 12 ? 1 : 0;
        if (gazeX == _gazeX && gazeY == _gazeY) return;
        _gazeX = gazeX;
        _gazeY = gazeY;
        InvalidateVisual();
    }

    private void OnWindowMouseLeave(object sender, MouseEventArgs e)
    {
        if (!ReferenceEquals(sender, _window) || !CanAnimate) return;
        _gazeX = 0;
        _gazeY = 0;
        InvalidateVisual();
    }
}
