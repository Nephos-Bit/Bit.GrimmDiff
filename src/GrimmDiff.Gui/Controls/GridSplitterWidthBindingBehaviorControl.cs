using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace GrimmDiff.Gui;

public static class GridSplitterWidthBindingBehaviorControl
{
    public static readonly DependencyProperty PanelWidthProperty = DependencyProperty.RegisterAttached(
        "PanelWidth", typeof(GridLength), typeof(GridSplitterWidthBindingBehaviorControl),
        new PropertyMetadata(new GridLength(0), OnPanelWidthChanged));

    public static readonly DependencyProperty TargetColumnIndexProperty = DependencyProperty.RegisterAttached(
        "TargetColumnIndex", typeof(int), typeof(GridSplitterWidthBindingBehaviorControl),
        new PropertyMetadata(-1, OnTargetColumnIndexChanged));

    public static GridLength GetPanelWidth(DependencyObject element)
    {
        return (GridLength)element.GetValue(PanelWidthProperty);
    }

    public static void SetPanelWidth(DependencyObject element, GridLength value)
    {
        element.SetValue(PanelWidthProperty, value);
    }

    public static int GetTargetColumnIndex(DependencyObject element)
    {
        return (int)element.GetValue(TargetColumnIndexProperty);
    }

    public static void SetTargetColumnIndex(DependencyObject element, int value)
    {
        element.SetValue(TargetColumnIndexProperty, value);
    }

    private static void OnPanelWidthChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        ApplyPanelWidth(dependencyObject as GridSplitter);
    }

    private static void OnTargetColumnIndexChanged(DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not GridSplitter splitter) return;
        splitter.Loaded -= OnLoaded;
        splitter.Loaded += OnLoaded;
        splitter.DragCompleted -= OnDragCompleted;
        splitter.DragCompleted += OnDragCompleted;
        ApplyPanelWidth(splitter);
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyPanelWidth(sender as GridSplitter);
    }

    private static void OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (sender is not GridSplitter splitter || !TryGetTargetColumn(splitter, out var column)) return;
        splitter.SetCurrentValue(PanelWidthProperty, new GridLength(column.ActualWidth));
        BindingOperations.GetBindingExpression(splitter, PanelWidthProperty)?.UpdateSource();
    }

    private static void ApplyPanelWidth(GridSplitter? splitter)
    {
        if (!TryGetTargetColumn(splitter, out var column)) return;
        column.Width = GetPanelWidth(splitter!);
    }

    private static bool TryGetTargetColumn(GridSplitter? splitter, out ColumnDefinition column)
    {
        column = null!;
        if (splitter?.Parent is not Grid grid) return false;
        var index = GetTargetColumnIndex(splitter);
        if (index < 0 || index >= grid.ColumnDefinitions.Count) return false;
        column = grid.ColumnDefinitions[index];
        return true;
    }
}
