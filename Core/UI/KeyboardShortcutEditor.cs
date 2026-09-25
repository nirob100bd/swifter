using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Swifter.Core.Config;

namespace Swifter.Core.UI;

public sealed class KeyboardShortcutEditor : Window
{
    private readonly Dictionary<string, string> _shortcuts;
    private string _editingAction = "";

    public KeyboardShortcutEditor()
    {
        Title = "Keyboard Shortcuts";
        Width = 560;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        _shortcuts = new Dictionary<string, string>(SettingsManager.Instance.Settings.Shortcuts);
        if (_shortcuts.Count == 0) LoadDefaults();
        BuildUI();
    }

    private void LoadDefaults()
    {
        _shortcuts["NewTab"] = "Ctrl+T";
        _shortcuts["CloseTab"] = "Ctrl+W";
        _shortcuts["ReopenTab"] = "Ctrl+Shift+T";
        _shortcuts["NextTab"] = "Ctrl+Tab";
        _shortcuts["PrevTab"] = "Ctrl+Shift+Tab";
        _shortcuts["FocusUrl"] = "Ctrl+L";
        _shortcuts["Reload"] = "F5";
        _shortcuts["HardReload"] = "Ctrl+Shift+R";
        _shortcuts["DevTools"] = "F12";
        _shortcuts["Fullscreen"] = "F11";
        _shortcuts["Find"] = "Ctrl+F";
        _shortcuts["Downloads"] = "Ctrl+J";
        _shortcuts["History"] = "Ctrl+H";
        _shortcuts["Bookmarks"] = "Ctrl+D";
        _shortcuts["Settings"] = "Ctrl+,";
        _shortcuts["ZoomIn"] = "Ctrl+=";
        _shortcuts["ZoomOut"] = "Ctrl+-";
        _shortcuts["ZoomReset"] = "Ctrl+0";
        _shortcuts["ReaderMode"] = "Ctrl+Shift+I";
        _shortcuts["CommandPalette"] = "Ctrl+Shift+P";
    }

    private void BuildUI()
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(32, 32, 36)),
            CornerRadius = new CornerRadius(12),
            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 66)),
            BorderThickness = new Thickness(1),
            Effect = new DropShadowEffect { BlurRadius = 30, ShadowDepth = 4, Opacity = 0.5, Color = Colors.Black },
            Padding = new Thickness(24)
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = "⌨️ Keyboard Shortcuts", FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 16) });
        var scrollViewer = new ScrollViewer { MaxHeight = 440, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var listStack = new StackPanel();
        foreach (var kv in _shortcuts)
        {
            var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = FormatActionName(kv.Key), FontSize = 13, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center });
            var shortcutBox = new TextBox
            {
                Text = kv.Value,
                FontSize = 12,
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromRgb(42, 42, 48)),
                Foreground = new SolidColorBrush(Color.FromRgb(120, 200, 255)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 6, 8, 6),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0),
                Tag = kv.Key
            };
            shortcutBox.GotKeyboardFocus += (_, _) => { _editingAction = kv.Key; shortcutBox.Text = "Press keys..."; };
            shortcutBox.PreviewKeyDown += (_, e) =>
            {
                e.Handled = true;
                var mods = Keyboard.Modifiers;
                if (mods == ModifierKeys.None && (e.Key == Key.Escape || e.Key == Key.Delete))
                {
                    _shortcuts[kv.Key] = "";
                    shortcutBox.Text = "(none)";
                    return;
                }
                if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl || e.Key == Key.LeftAlt || e.Key == Key.RightAlt || e.Key == Key.LeftShift || e.Key == Key.RightShift || e.Key == Key.LWin || e.Key == Key.RWin) return;
                var combo = FormatKeyCombo(mods, e.Key);
                _shortcuts[kv.Key] = combo;
                shortcutBox.Text = combo;
            };
            Grid.SetColumn(shortcutBox, 1);
            row.Children.Add(shortcutBox);
            var resetBtn = new Button { Content = "↺", Width = 24, Height = 24, FontSize = 12, Background = Brushes.Transparent, Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 170)), BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
            Grid.SetColumn(resetBtn, 2);
            row.Children.Add(resetBtn);
            listStack.Children.Add(row);
        }
        scrollViewer.Content = listStack;
        stack.Children.Add(scrollViewer);
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var resetAllBtn = new Button { Content = "Reset All", Width = 100, Height = 32, FontSize = 12, Background = new SolidColorBrush(Color.FromRgb(50, 50, 56)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Margin = new Thickness(0, 0, 8, 0), Cursor = Cursors.Hand };
        resetAllBtn.Click += (_, _) => { _shortcuts.Clear(); LoadDefaults(); };
        btnPanel.Children.Add(resetAllBtn);
        var saveBtn = new Button { Content = "Save", Width = 80, Height = 32, FontSize = 12, Background = new SolidColorBrush(Color.FromRgb(0, 120, 212)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
        saveBtn.Click += (_, _) => Save();
        btnPanel.Children.Add(saveBtn);
        stack.Children.Add(btnPanel);
        border.Child = stack;
        Content = border;
        MouseLeftButtonDown += (_, _) => DragMove();
    }

    private static string FormatActionName(string key) => string.Join(" ", System.Text.RegularExpressions.Regex.Split(key, "(?=[A-Z])"));

    private static string FormatKeyCombo(ModifierKeys mods, Key key)
    {
        var parts = new List<string>();
        if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (mods.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }

    private void Save()
    {
        SettingsManager.Instance.Settings.Shortcuts = new Dictionary<string, string>(_shortcuts);
        SettingsManager.Instance.Save();
        Close();
    }
}