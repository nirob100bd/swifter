using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Swifter.Core.UI;

public sealed class CanvasWhiteboardModal : Window
{
    private readonly InkCanvas _canvas;
    private Color _currentColor = Colors.White;
    private double _strokeWidth = 2;

    public CanvasWhiteboardModal()
    {
        Title = "Whiteboard";
        Width = 900;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(28, 28, 32)),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 66)),
            BorderThickness = new Thickness(1),
            Effect = new DropShadowEffect { BlurRadius = 30, ShadowDepth = 6, Opacity = 0.5, Color = Colors.Black }
        };
        var dock = new DockPanel();
        var toolbar = CreateToolbar();
        DockPanel.SetDock(toolbar, Dock.Top);
        dock.Children.Add(toolbar);
        _canvas = new InkCanvas
        {
            Background = new SolidColorBrush(Color.FromRgb(36, 36, 42)),
            DefaultDrawingAttributes = new DrawingAttributes
            {
                Color = _currentColor,
                Width = _strokeWidth,
                Height = _strokeWidth,
                FitToCurve = true
            }
        };
        dock.Children.Add(_canvas);
        border.Child = dock;
        Content = border;
    }

    private Border CreateToolbar()
    {
        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Background = new SolidColorBrush(Color.FromRgb(24, 24, 28)),
            Margin = new Thickness(0)
        };
        var toolbarBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(24, 24, 28)),
            Padding = new Thickness(12, 6, 12, 6),
            Child = toolbar
        };
        toolbar.MouseLeftButtonDown += (_, _) => DragMove();
        toolbar.Children.Add(new TextBlock { Text = "🎨 Whiteboard", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) });
        Color[] colors = { Colors.White, Colors.Red, Colors.Orange, Colors.Yellow, Colors.LimeGreen, Colors.DodgerBlue, Colors.MediumPurple, Colors.HotPink };
        foreach (var color in colors)
        {
            var btn = new Button
            {
                Width = 24, Height = 24,
                Background = new SolidColorBrush(color),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(2, 0, 2, 0),
                Cursor = Cursors.Hand,
                Tag = color
            };
            btn.Click += (_, e) =>
            {
                if (e.Source is Button b && b.Tag is Color c)
                {
                    _currentColor = c;
                    _canvas.DefaultDrawingAttributes.Color = c;
                }
            };
            toolbar.Children.Add(btn);
        }
        toolbar.Children.Add(new Separator { Width = 1, Height = 20, Background = new SolidColorBrush(Color.FromRgb(60, 60, 66)), Margin = new Thickness(8, 0, 8, 0) });
        var sizes = new[] { ("S", 1.0), ("M", 3.0), ("L", 6.0), ("XL", 10.0) };
        foreach (var (label, size) in sizes)
        {
            var btn = new Button
            {
                Content = label, Width = 32, Height = 26, FontSize = 11,
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 56)),
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                Margin = new Thickness(2, 0, 2, 0), Cursor = Cursors.Hand, Tag = size
            };
            btn.Click += (_, e) => { if (e.Source is Button b && b.Tag is double s) { _strokeWidth = s; _canvas.DefaultDrawingAttributes.Width = s; _canvas.DefaultDrawingAttributes.Height = s; } };
            toolbar.Children.Add(btn);
        }
        toolbar.Children.Add(new Separator { Width = 1, Height = 20, Background = new SolidColorBrush(Color.FromRgb(60, 60, 66)), Margin = new Thickness(8, 0, 8, 0) });
        var eraserBtn = new Button { Content = "🧹 Eraser", FontSize = 12, Background = new SolidColorBrush(Color.FromRgb(50, 50, 56)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(8, 4, 8, 4), Cursor = Cursors.Hand, Margin = new Thickness(2, 0, 2, 0) };
        eraserBtn.Click += (_, _) => _canvas.EditingMode = _canvas.EditingMode == InkCanvasEditingMode.EraseByPoint ? InkCanvasEditingMode.Ink : InkCanvasEditingMode.EraseByPoint;
        toolbar.Children.Add(eraserBtn);
        var clearBtn = new Button { Content = "🗑 Clear", FontSize = 12, Background = new SolidColorBrush(Color.FromRgb(50, 50, 56)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(8, 4, 8, 4), Cursor = Cursors.Hand, Margin = new Thickness(2, 0, 2, 0) };
        clearBtn.Click += (_, _) => _canvas.Strokes.Clear();
        toolbar.Children.Add(clearBtn);
        var closeBtn = new Button { Content = "✕", Width = 28, Height = 28, FontSize = 12, Background = new SolidColorBrush(Color.FromRgb(196, 43, 28)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(8, 0, 0, 0) };
        closeBtn.Click += (_, _) => Close();
        toolbar.Children.Add(closeBtn);
        return toolbarBorder;
    }
}