using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Swifter.Core.TabEngine;

namespace Swifter.Core.UI;

public sealed class TabStripControl : UserControl
{
    private readonly StackPanel _tabPanel;
    private readonly ScrollViewer _scrollViewer;
    private readonly Button _newTabButton;

    public event EventHandler<TabData>? TabCreated;
    public event EventHandler<TabData>? TabClosed;
    public event EventHandler<TabData>? TabSelected;

    public TabStripControl()
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(28, 28, 32)),
            Height = 38,
            Padding = new Thickness(4, 2, 4, 2)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _tabPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _scrollViewer.Content = _tabPanel;
        Grid.SetColumn(_scrollViewer, 0);
        grid.Children.Add(_scrollViewer);

        _newTabButton = new Button
        {
            Content = "+",
            Width = 32,
            Height = 32,
            FontSize = 18,
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 170)),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 2, 0)
        };
        _newTabButton.Click += (_, _) =>
        {
            var tab = TabManager.Instance.CreateTab();
            TabCreated?.Invoke(this, tab);
        };
        Grid.SetColumn(_newTabButton, 1);
        grid.Children.Add(_newTabButton);

        border.Child = grid;
        Content = border;

        TabManager.Instance.Tabs.CollectionChanged += (_, _) => RebuildTabs();
        TabManager.Instance.ActiveTabChanged += (_, _) => RebuildTabs();

        RebuildTabs();
    }

    private void RebuildTabs()
    {
        _tabPanel.Children.Clear();
        foreach (var tab in TabManager.Instance.Tabs)
        {
            _tabPanel.Children.Add(CreateTabElement(tab));
        }
    }

    private Border CreateTabElement(TabData tab)
    {
        var isActive = tab.IsActive;
        var bg = isActive ? Color.FromRgb(42, 42, 48) : Color.FromRgb(32, 32, 36);
        var border = new Border
        {
            Background = new SolidColorBrush(bg),
            CornerRadius = new CornerRadius(8, 8, 0, 0),
            Margin = new Thickness(1, 0, 1, 0),
            MinWidth = 120,
            MaxWidth = 240,
            Padding = new Thickness(8, 4, 8, 4),
            Cursor = Cursors.Hand,
            Tag = tab
        };

        if (isActive)
        {
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));
            border.BorderThickness = new Thickness(0, 2, 0, 0);
        }

        if (!string.IsNullOrEmpty(tab.GroupColor))
        {
            var color = (Color)ColorConverter.ConvertFromString(tab.GroupColor);
            border.BorderBrush = new SolidColorBrush(color);
            border.BorderThickness = new Thickness(0, 2, 0, 0);
        }

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        if (tab.IsHibernate)
        {
            var hibernateIcon = new TextBlock
            {
                Text = "💤",
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };
            Grid.SetColumn(hibernateIcon, 0);
            grid.Children.Add(hibernateIcon);
        }

        var titleText = new TextBlock
        {
            Text = tab.Title,
            FontSize = 12,
            Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(160, 160, 170)),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 4, 0)
        };
        Grid.SetColumn(titleText, 1);
        grid.Children.Add(titleText);

        var closeBtn = new Button
        {
            Content = "✕",
            Width = 20,
            Height = 20,
            FontSize = 10,
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 130)),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0),
            Tag = tab
        };
        closeBtn.Click += (_, _) =>
        {
            TabManager.Instance.CloseTab(tab.Id);
            TabClosed?.Invoke(this, tab);
        };
        closeBtn.MouseEnter += (_, _) => closeBtn.Foreground = Brushes.White;
        closeBtn.MouseLeave += (_, _) => closeBtn.Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 130));
        Grid.SetColumn(closeBtn, 2);
        grid.Children.Add(closeBtn);

        border.Child = grid;
        border.MouseLeftButtonDown += (_, _) =>
        {
            TabManager.Instance.SetActive(tab.Id);
            TabSelected?.Invoke(this, tab);
        };
        border.MouseEnter += (_, _) =>
        {
            if (!isActive) border.Background = new SolidColorBrush(Color.FromRgb(38, 38, 44));
        };
        border.MouseLeave += (_, _) =>
        {
            border.Background = new SolidColorBrush(bg);
        };
        border.MouseRightButtonUp += (_, e) =>
        {
            var menu = new ContextMenu();
            var duplicate = new MenuItem { Header = "Duplicate Tab" };
            duplicate.Click += (_, _) => TabManager.Instance.DuplicateTab(tab.Id);
            menu.Items.Add(duplicate);
            var pin = new MenuItem { Header = tab.IsPinned ? "Unpin Tab" : "Pin Tab" };
            pin.Click += (_, _) => TabManager.Instance.PinTab(tab.Id);
            menu.Items.Add(pin);
            var mute = new MenuItem { Header = "Mute Tab" };
            menu.Items.Add(mute);
            var close = new MenuItem { Header = "Close Tab" };
            close.Click += (_, _) => { TabManager.Instance.CloseTab(tab.Id); TabClosed?.Invoke(this, tab); };
            menu.Items.Add(close);
            menu.IsOpen = true;
            e.Handled = true;
        };
        ToolTipService.SetToolTip(border, tab.Title);
        return border;
    }
}