using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GrimmDiff.Gui;

public sealed class ReviewMascotControl : FrameworkElement
{
    private const int PixelSize = 2;
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

    public static readonly DependencyProperty OutlineBrushProperty = RegisterBrush(nameof(OutlineBrush));
    public static readonly DependencyProperty BodyBrushProperty = RegisterBrush(nameof(BodyBrush));
    public static readonly DependencyProperty AccentBrushProperty = RegisterBrush(nameof(AccentBrush));
    public static readonly DependencyProperty HighlightBrushProperty = RegisterBrush(nameof(HighlightBrush));

    public bool IsAnalyzing
    {
        get => (bool)GetValue(IsAnalyzingProperty);
        set => SetValue(IsAnalyzingProperty, value);
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
        if (OutlineBrush is null || BodyBrush is null || AccentBrush is null || HighlightBrush is null) return;

        var animated = CanAnimate;
        var bob = animated && (IsAnalyzing ? _frame % 4 >= 2 : _frame % 6 >= 3) ? 1 : 0;
        var blinking = animated && !IsAnalyzing && _frame % 12 == 11;

        // All coordinates are whole pixels on an 18 × 18 grid.
        PixelRect(context, OutlineBrush, 8, 1, 2, 2, bob);
        PixelRect(context, AccentBrush, 8, 1, 2, 1, bob);
        PixelRect(context, OutlineBrush, 4, 2, 10, 1, bob);
        PixelRect(context, OutlineBrush, 3, 3, 12, 12, bob);
        PixelRect(context, OutlineBrush, 2, 6, 14, 6, bob);
        PixelRect(context, BodyBrush, 4, 4, 10, 10, bob);
        PixelRect(context, AccentBrush, 4, 4, 10, 1, bob);
        PixelRect(context, AccentBrush, 2, 7, 1, 4, bob);
        PixelRect(context, AccentBrush, 15, 7, 1, 4, bob);
        PixelRect(context, OutlineBrush, 4, 6, 10, 6, bob);
        PixelRect(context, BodyBrush, 5, 7, 8, 4, bob);
        PixelRect(context, HighlightBrush, 5, 12, 1, 1, bob);
        PixelRect(context, HighlightBrush, 12, 12, 1, 1, bob);
        PixelRect(context, OutlineBrush, 7, 15, 4, 1, bob);
        PixelRect(context, AccentBrush, 8, 16, 2, 1, bob);

        if (blinking)
        {
            PixelRect(context, HighlightBrush, 6, 8, 2, 1, bob);
            PixelRect(context, HighlightBrush, 10, 8, 2, 1, bob);
        }
        else
        {
            PixelRect(context, HighlightBrush, 5, 7, 3, 3, bob);
            PixelRect(context, HighlightBrush, 10, 7, 3, 3, bob);
            PixelRect(context, OutlineBrush, 6 + _gazeX, 8 + _gazeY, 1, 1, bob);
            PixelRect(context, OutlineBrush, 11 + _gazeX, 8 + _gazeY, 1, 1, bob);
        }

        if (animated && IsAnalyzing)
        {
            PixelRect(context, HighlightBrush, 5 + _frame % 7, 4, 2, 1, bob);
            PixelRect(context, AccentBrush, 8 + _frame % 3, 13, 2, 1, bob);
        }
    }

    private static DependencyProperty RegisterBrush(string name)
    {
        return DependencyProperty.Register(name, typeof(Brush), typeof(ReviewMascotControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    }

    private static void PixelRect(DrawingContext context, Brush brush, int x, int y, int width, int height, int bob)
    {
        context.DrawRectangle(brush, null,
            new Rect(x * PixelSize, (y + bob) * PixelSize, width * PixelSize, height * PixelSize));
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
