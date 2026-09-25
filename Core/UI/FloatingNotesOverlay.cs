using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Swifter.Core.UI;

public sealed class FloatingNotesOverlay : Window
{
    private readonly TextBox _notesBox;
    private string _currentNote = "";

    public event EventHandler<string>? NoteSaved;

    public FloatingNotesOverlay()
    {
        Title = "Quick Notes";
        Width = 320;
        Height = 280;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = SystemParameters.WorkArea.Right - 360;
        Top = SystemParameters.WorkArea.Top + 80;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(230, 32, 32, 36)),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 66)),
            BorderThickness = new Thickness(1),
            Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 4, Opacity = 0.5, Color = Colors.Black },
            Padding = new Thickness(12)
        };
        var stack = new StackPanel();
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.Children.Add(new TextBlock { Text = "📝 Quick Notes", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
        var closeBtn = new Button { Content = "✕", Width = 24, Height = 24, FontSize = 10, Background = Brushes.Transparent, Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 170)), BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
        closeBtn.Click += (_, _) => { SaveAndClose(); };
        Grid.SetColumn(closeBtn, 1);
        header.Children.Add(closeBtn);
        header.MouseLeftButtonDown += (_, _) => DragMove();
        stack.Children.Add(header);
        _notesBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(24, 24, 28)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 8, 0, 0),
            MinHeight = 180,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        stack.Children.Add(_notesBox);
        border.Child = stack;
        Content = border;
        LoadNote();
    }

    private void LoadNote()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "quicknotes.txt");
        if (File.Exists(path)) _notesBox.Text = File.ReadAllText(path);
    }

    private void SaveAndClose()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "quicknotes.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, _notesBox.Text);
        NoteSaved?.Invoke(this, _notesBox.Text);
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "quicknotes.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, _notesBox.Text);
        base.OnClosed(e);
    }
}