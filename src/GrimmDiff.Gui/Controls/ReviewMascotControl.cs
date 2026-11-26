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

        if (option.Id != MascotCatalog.DefaultId)
        {
            DrawVariant(context, option.Id, outline, body, accent, highlight, option.Detail);
            return;
        }

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

    private void DrawVariant(DrawingContext context, string variantId, Brush o, Brush b, Brush a, Brush h, Brush? detail)
    {
        var animated = CanAnimate;
        var bob = animated && (IsAnalyzing ? _frame % 4 >= 2 : _frame % 6 >= 3) ? 1 : 0;
        var blinking = animated && !IsAnalyzing && _frame % 12 == 11;
        void P(Brush brush, int x, int y, int width, int height) => PixelRect(context, brush, x, y, width, height, bob);

        switch (variantId)
        {
            case "ember": // Fox: pointed ears, pale muzzle and a small tail.
                P(o, 2, 1, 4, 6); P(o, 12, 1, 4, 6); P(a, 3, 2, 2, 3); P(a, 13, 2, 2, 3);
                P(o, 3, 5, 12, 9); P(b, 4, 6, 10, 7); P(a, 4, 6, 10, 3);
                P(h, 5, 10, 8, 3); P(o, 8, 11, 2, 1); P(o, 15, 11, 2, 3); P(a, 16, 10, 1, 2);
                P(o, 5, 14, 3, 2); P(o, 10, 14, 3, 2);
                Eyes(5, 7, 11, 7);
                break;
            case "luma": // Owl: round head, ear tufts and broad wings.
                P(o, 3, 2, 3, 4); P(o, 12, 2, 3, 4); P(a, 4, 3, 2, 2); P(a, 12, 3, 2, 2);
                P(o, 4, 4, 10, 11); P(b, 5, 5, 8, 9); P(a, 2, 8, 3, 5); P(a, 13, 8, 3, 5);
                P(h, 5, 7, 3, 4); P(h, 10, 7, 3, 4); P(o, 8, 11, 2, 2); P(a, 6, 13, 6, 2);
                P(o, 5, 15, 3, 1); P(o, 10, 15, 3, 1);
                Eyes(6, 8, 11, 8);
                break;
            case "bolt": // Robot: antenna, square faceplate and two feet.
                P(o, 8, 1, 2, 3); P(h, 8, 1, 2, 1); P(o, 3, 4, 12, 10);
                P(b, 4, 5, 10, 8); P(a, 5, 6, 8, 5); P(o, 2, 8, 1, 4); P(o, 15, 8, 1, 4);
                P(h, 6, 7, 2, 2); P(h, 10, 7, 2, 2); P(o, 7, 11, 4, 1);
                P(o, 5, 14, 3, 2); P(o, 10, 14, 3, 2); P(h, 6, 15, 2, 1); P(h, 10, 15, 2, 1);
                Eyes(6, 7, 10, 7);
                break;
            case "sprig": // Sprout: two leaves above a face in a flowerpot.
                P(o, 8, 3, 2, 5); P(o, 3, 2, 5, 4); P(o, 10, 1, 5, 5);
                P(a, 4, 3, 3, 2); P(a, 11, 2, 3, 3); P(o, 4, 7, 10, 7);
                P(b, 5, 8, 8, 5); P(h, 6, 10, 6, 2); P(o, 3, 13, 12, 2);
                P(a, 4, 15, 10, 1); P(o, 5, 16, 8, 1);
                Eyes(6, 9, 11, 9);
                break;
            case "wisp": // Ghost: rounded head, raised arms and a tapering tail.
                P(o, 6, 2, 6, 1); P(o, 4, 3, 10, 2); P(o, 3, 5, 12, 9);
                P(b, 4, 5, 10, 8); P(a, 4, 5, 10, 2); P(o, 1, 8, 2, 4); P(o, 15, 8, 2, 4);
                P(a, 2, 9, 2, 2); P(a, 14, 9, 2, 2); P(o, 5, 14, 9, 1);
                P(b, 6, 14, 7, 2); P(a, 8, 16, 4, 1); P(h, 5, 7, 2, 1);
                Eyes(6, 8, 11, 8);
                break;
            case "sgt-schnabel": // Yellow rubber duck in profile with an orange bill.
                P(o, 5, 2, 7, 1); P(o, 4, 3, 9, 1); P(o, 3, 4, 10, 6);
                P(b, 4, 4, 8, 6); P(h, 5, 4, 5, 1);
                P(o, 1, 9, 4, 4); P(o, 2, 10, 12, 5); P(b, 2, 10, 2, 2);
                P(b, 3, 10, 10, 4); P(h, 5, 11, 6, 2); P(o, 4, 15, 9, 1);
                P(o, 12, 7, 5, 3); P(a, 13, 7, 3, 1); P(a, 12, 8, 5, 1);
                if (blinking) P(o, 9, 6, 2, 1);
                else { P(h, 9, 5, 2, 2); P(o, 9 + _gazeX, 5 + _gazeY, 1, 1); }
                break;
            case "berta": // White hen with a red comb and an orange beak.
                P(o, 7, 1, 2, 4); P(o, 10, 1, 2, 4); P(o, 5, 2, 2, 4);
                P(a, 6, 3, 5, 3); P(a, 8, 2, 3, 2);
                P(o, 4, 5, 10, 9); P(b, 5, 6, 8, 7);
                P(o, 2, 10, 3, 4); P(o, 13, 10, 3, 4);
                P(b, 3, 11, 2, 2); P(b, 13, 11, 2, 2);
                P(o, 5, 13, 8, 3); P(b, 6, 13, 6, 2);
                P(o, 5, 16, 3, 1); P(o, 10, 16, 3, 1);
                if (detail is not null) { P(detail, 8, 10, 2, 1); P(detail, 9, 11, 1, 1); }
                Eyes(6, 7, 11, 7);
                break;
        }

        if (animated && IsAnalyzing) P(h, 8 + _frame % 3, 4, 2, 1);

        void Eyes(int leftX, int leftY, int rightX, int rightY)
        {
            if (blinking)
            {
                P(o, leftX, leftY + 1, 2, 1); P(o, rightX, rightY + 1, 2, 1);
            }
            else
            {
                P(h, leftX, leftY, 2, 2); P(h, rightX, rightY, 2, 2);
                P(o, leftX + _gazeX, leftY + _gazeY, 1, 1);
                P(o, rightX + _gazeX, rightY + _gazeY, 1, 1);
            }
        }
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
