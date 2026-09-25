using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Swifter.Core.Config;
using Swifter.Core.Storage;

namespace Swifter.Core.UI;

public sealed class AddressBarControl : UserControl
{
    private readonly TextBox _addressBox = null!;
    private readonly Border _container = null!;
    private readonly Popup _suggestionsPopup = null!;
    private readonly StackPanel _suggestionsPanel = null!;
    private readonly Border _securityIndicator = null!;

    public event EventHandler<string>? NavigationRequested;

    public AddressBarControl()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        _securityIndicator = new Border
        {
            Width = 24,
            Height = 24,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.FromRgb(63, 185, 80)),
            Margin = new Thickness(8, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var lockIcon = new TextBlock
        {
            Text = "🔒",
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _securityIndicator.Child = lockIcon;
        Grid.SetColumn(_securityIndicator, 0);
        grid.Children.Add(_securityIndicator);

        _addressBox = new TextBox
        {
            FontSize = 13,
            FontFamily = new FontFamily("Segoe UI Variable"),
            Foreground = Brushes.White,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 6, 8, 6),
            VerticalContentAlignment = VerticalAlignment.Center,
            CaretBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212))
        };
        _addressBox.GotFocus += (_, _) =>
        {
            _addressBox.SelectAll();
            _container.Background = new SolidColorBrush(Color.FromRgb(50, 50, 56));
        };
        _addressBox.LostFocus += (_, _) =>
        {
            _container.Background = new SolidColorBrush(Color.FromRgb(42, 42, 48));
            _suggestionsPopup.IsOpen = false;
        };
        _addressBox.KeyDown += OnAddressKeyDown;
        _addressBox.TextChanged += OnTextChanged;
        Grid.SetColumn(_addressBox, 1);

        _suggestionsPanel = new StackPanel();
        _suggestionsPopup = new Popup
        {
            PlacementTarget = _addressBox,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
            AllowsTransparency = true,
            StaysOpen = false
        };
        _suggestionsPopup.Child = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(36, 36, 42)),
            CornerRadius = new CornerRadius(8),
            BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 66)),
            BorderThickness = new Thickness(1),
            Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 2, Opacity = 0.4, Color = Colors.Black },
            MaxHeight = 400,
            Child = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _suggestionsPanel
            }
        };

        var innerGrid = new Grid();
        innerGrid.Children.Add(_addressBox);
        innerGrid.Children.Add(_suggestionsPopup);
        Grid.SetColumn(innerGrid, 1);
        grid.Children.Add(innerGrid);

        _container = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(42, 42, 48)),
            CornerRadius = new CornerRadius(20),
            Margin = new Thickness(8, 4, 8, 4),
            Padding = new Thickness(0)
        };
        _container.Child = grid;
        Content = _container;
    }

    public void UpdateUrl(string url)
    {
        _addressBox.Text = url;
        UpdateSecurityIndicator(url);
    }

    public void FocusAddressBar()
    {
        _addressBox.Focus();
        _addressBox.SelectAll();
    }

    private void UpdateSecurityIndicator(string url)
    {
        var isSecure = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        _securityIndicator.Background = isSecure
            ? new SolidColorBrush(Color.FromRgb(63, 185, 80))
            : new SolidColorBrush(Color.FromRgb(248, 81, 73));
    }

    private async void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var text = _addressBox.Text;
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
        {
            _suggestionsPopup.IsOpen = false;
            return;
        }

        using var historyDb = new HistoryDatabase();
        var history = await historyDb.SearchAsync(text, 8);
        _suggestionsPanel.Children.Clear();
        foreach (var entry in history)
        {
            var item = CreateSuggestionItem(entry.Url, entry.Title, entry.VisitCount);
            _suggestionsPanel.Children.Add(item);
        }
        _suggestionsPopup.IsOpen = _suggestionsPanel.Children.Count > 0;
    }

    private Border CreateSuggestionItem(string url, string title, int visitCount)
    {
        var border = new Border
        {
            Padding = new Thickness(12, 8, 12, 8),
            Cursor = Cursors.Hand,
            Tag = url
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 13,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        stack.Children.Add(new TextBlock
        {
            Text = url,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 130)),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        border.Child = stack;
        border.MouseLeftButtonDown += (_, _) =>
        {
            NavigationRequested?.Invoke(this, url);
            _suggestionsPopup.IsOpen = false;
        };
        border.MouseEnter += (_, _) => border.Background = new SolidColorBrush(Color.FromRgb(50, 50, 56));
        border.MouseLeave += (_, _) => border.Background = Brushes.Transparent;
        return border;
    }

    private void OnAddressKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var text = _addressBox.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                NavigationRequested?.Invoke(this, text);
                _suggestionsPopup.IsOpen = false;
            }
            e.Handled = true;
        }
        if (e.Key == Key.Escape)
        {
            _suggestionsPopup.IsOpen = false;
            _addressBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            e.Handled = true;
        }
    }
}