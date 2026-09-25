using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;
using Swifter.Core.TabEngine;

namespace Swifter.Core.UI;

public sealed class SplitViewManager
{
    private static SplitViewManager? _instance;
    private SplitOrientation _orientation = SplitOrientation.Vertical;
    private Grid? _container;
    private WebView2? _primaryView;
    private WebView2? _secondaryView;

    public static SplitViewManager Instance => _instance ??= new SplitViewManager();

    public bool IsSplitActive => _primaryView != null && _secondaryView != null;

    private SplitViewManager()
    {
    }

    public void ActivateSplit(Grid container, WebView2 primary, WebView2 secondary, SplitOrientation orientation = SplitOrientation.Vertical)
    {
        _container = container;
        _primaryView = primary;
        _secondaryView = secondary;
        _orientation = orientation;
        container.ColumnDefinitions.Clear();
        container.RowDefinitions.Clear();
        if (orientation == SplitOrientation.Vertical)
        {
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(primary, 0);
            Grid.SetColumn(secondary, 2);
            var splitter = new GridSplitter { Width = 4, HorizontalAlignment = HorizontalAlignment.Stretch, Background = new SolidColorBrush(Color.FromRgb(60, 60, 66)) };
            Grid.SetColumn(splitter, 1);
            container.Children.Add(splitter);
        }
        else
        {
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) });
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(primary, 0);
            Grid.SetRow(secondary, 2);
            var splitter = new GridSplitter { Height = 4, VerticalAlignment = VerticalAlignment.Stretch, Background = new SolidColorBrush(Color.FromRgb(60, 60, 66)) };
            Grid.SetRow(splitter, 1);
            container.Children.Add(splitter);
        }
        primary.Visibility = Visibility.Visible;
        secondary.Visibility = Visibility.Visible;
    }

    public void DeactivateSplit()
    {
        if (_container == null || _secondaryView == null) return;
        _secondaryView.Visibility = Visibility.Collapsed;
        _container.ColumnDefinitions.Clear();
        _container.RowDefinitions.Clear();
        _container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(_primaryView!, 0);
        _secondaryView = null;
    }

    public void ToggleOrientation()
    {
        _orientation = _orientation == SplitOrientation.Vertical ? SplitOrientation.Horizontal : SplitOrientation.Vertical;
        if (_container != null && _primaryView != null && _secondaryView != null)
            ActivateSplit(_container, _primaryView, _secondaryView, _orientation);
    }
}

public enum SplitOrientation
{
    Vertical,
    Horizontal
}