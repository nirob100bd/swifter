using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Swifter.Core.UI;

public sealed class CommandPaletteModal : Window
{
    private readonly TextBox _searchBox;
    private readonly StackPanel _resultsPanel;
    private readonly List<CommandEntry> _commands;
    private string _selectedCommandId = "";

    public string SelectedCommandId => _selectedCommandId;

    public CommandPaletteModal()
    {
        Title = "Command Palette";
        Width = 560;
        Height = 420;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        _commands = LoadCommands();
        _searchBox = new TextBox();
        _resultsPanel = new StackPanel();
        BuildUI();
    }

    private List<CommandEntry> LoadCommands()
    {
        return new List<CommandEntry>
        {
            new() { Id = "new_tab", Name = "New Tab", Category = "Tabs", Shortcut = "Ctrl+T" },
            new() { Id = "close_tab", Name = "Close Tab", Category = "Tabs", Shortcut = "Ctrl+W" },
            new() { Id = "reopen_tab", Name = "Reopen Closed Tab", Category = "Tabs", Shortcut = "Ctrl+Shift+T" },
            new() { Id = "next_tab", Name = "Next Tab", Category = "Tabs", Shortcut = "Ctrl+Tab" },
            new() { Id = "focus_url", Name = "Focus Address Bar", Category = "Navigation", Shortcut = "Ctrl+L" },
            new() { Id = "reload", Name = "Reload Page", Category = "Navigation", Shortcut = "F5" },
            new() { Id = "devtools", Name = "Open DevTools", Category = "Developer", Shortcut = "F12" },
            new() { Id = "fullscreen", Name = "Toggle Fullscreen", Category = "View", Shortcut = "F11" },
            new() { Id = "downloads", Name = "Show Downloads", Category = "Pages", Shortcut = "Ctrl+J" },
            new() { Id = "history", Name = "Show History", Category = "Pages", Shortcut = "Ctrl+H" },
            new() { Id = "bookmarks", Name = "Show Bookmarks", Category = "Pages", Shortcut = "Ctrl+B" },
            new() { Id = "settings", Name = "Open Settings", Category = "Pages", Shortcut = "Ctrl+," },
            new() { Id = "reader_mode", Name = "Reader Mode", Category = "View", Shortcut = "Ctrl+Shift+R" },
            new() { Id = "focus_mode", Name = "Focus Mode", Category = "View", Shortcut = "Ctrl+Shift+F" },
            new() { Id = "clear_data", Name = "Clear Browsing Data", Category = "Privacy", Shortcut = "" },
            new() { Id = "screenshot", Name = "Capture Screenshot", Category = "Tools", Shortcut = "Ctrl+Shift+S" },
            new() { Id = "find", Name = "Find on Page", Category = "Navigation", Shortcut = "Ctrl+F" },
            new() { Id = "zoom_in", Name = "Zoom In", Category = "View", Shortcut = "Ctrl+=" },
            new() { Id = "zoom_out", Name = "Zoom Out", Category = "View", Shortcut = "Ctrl+-" },
            new() { Id = "zoom_reset", Name = "Reset Zoom", Category = "View", Shortcut = "Ctrl+0" }
        };
    }

    private void BuildUI()
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 34)),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Color.FromRgb(55, 55, 62)),
            BorderThickness = new Thickness(1),
            Effect = new DropShadowEffect { BlurRadius = 40, ShadowDepth = 8, Opacity = 0.6, Color = Colors.Black }
        };
        var stack = new StackPanel();
        _searchBox = new TextBox
        {
            FontSize = 16,
            Foreground = Brushes.White,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(16, 14, 16, 14),
            CaretBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212))
        };
        _searchBox.TextChanged += (_, _) => FilterCommands(_searchBox.Text);
        _searchBox.KeyDown += OnSearchKeyDown;
        stack.Children.Add(new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(55, 55, 62)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = _searchBox
        });
        var scrollViewer = new ScrollViewer { MaxHeight = 340, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        _resultsPanel = new StackPanel { Margin = new Thickness(4) };
        scrollViewer.Content = _resultsPanel;
        stack.Children.Add scrollViewer;
        border.Child = stack;
        Content = border;
        Loaded += (_, _) => _searchBox.Focus();
        Deactivated += (_, _) => Close();
        FilterCommands("");
    }

    private void FilterCommands(string query)
    {
        _resultsPanel.Children.Clear();
        var filtered = string.IsNullOrWhiteSpace(query) ? _commands : _commands.Where(c =>
            c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            c.Category.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        string lastCategory = "";
        foreach (var cmd in filtered)
        {
            if (cmd.Category != lastCategory)
            {
                lastCategory = cmd.Category;
                _resultsPanel.Children.Add(new TextBlock
                {
                    Text = cmd.Category.ToUpper(),
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 110)),
                    Margin = new Thickness(12, 8, 12, 4)
                });
            }
            var item = new Border
            {
                Padding = new Thickness(12, 8, 12, 8),
                CornerRadius = new CornerRadius(6),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = cmd
            };
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.Children.Add(new TextBlock { Text = cmd.Name, FontSize = 13, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center });
            var shortcut = new TextBlock { Text = cmd.Shortcut, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 130)), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(shortcut, 1);
            g.Children.Add(shortcut);
            item.Child = g;
            item.MouseLeftButtonDown += (_, _) => { _selectedCommandId = cmd.Id; DialogResult = true; Close(); };
            item.MouseEnter += (_, _) => item.Background = new SolidColorBrush(Color.FromRgb(45, 45, 52));
            item.MouseLeave += (_, _) => item.Background = Brushes.Transparent;
            _resultsPanel.Children.Add(item);
        }
    }

    private void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(); e.Handled = true; }
        if (e.Key == Key.Enter && _resultsPanel.Children.Count > 0)
        {
            foreach (var child in _resultsPanel.Children)
            {
                if (child is Border b && b.Tag is CommandEntry cmd)
                {
                    _selectedCommandId = cmd.Id;
                    DialogResult = true;
                    Close();
                    break;
                }
            }
            e.Handled = true;
        }
    }
}

public sealed class CommandEntry
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Shortcut { get; set; } = "";
}