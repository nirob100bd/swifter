using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Swifter.Core.TabEngine;

namespace Swifter.Core.UI;

public sealed class ProfileSwitcherModal : Window
{
    public SandboxProfile? SelectedProfile { get; private set; }

    public ProfileSwitcherModal()
    {
        Title = "Switch Profile";
        Width = 400;
        Height = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        BuildUI();
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
        stack.Children.Add(new TextBlock { Text = "👤 Switch Profile", FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 16) });
        foreach (var profile in SandboxManager.Instance.Profiles)
        {
            var item = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(42, 42, 48)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 4, 0, 4),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = profile
            };
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var avatar = new Border { Width = 36, Height = 36, CornerRadius = new CornerRadius(18), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(profile.Color)) };
            var letter = new TextBlock { Text = profile.Name[0].ToString(), FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            avatar.Child = letter;
            Grid.SetColumn(avatar, 0);
            g.Children.Add(avatar);
            var info = new StackPanel { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            info.Children.Add(new TextBlock { Text = profile.Name, FontSize = 14, FontWeight = FontWeights.Medium, Foreground = Brushes.White });
            if (profile.IsDefault) info.Children.Add(new TextBlock { Text = "Default", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(63, 185, 80)) });
            Grid.SetColumn(info, 1);
            g.Children.Add(info);
            item.Child = g;
            item.MouseLeftButtonDown += (_, _) => { SelectedProfile = profile; DialogResult = true; Close(); };
            item.MouseEnter += (_, _) => item.Background = new SolidColorBrush(Color.FromRgb(55, 55, 62));
            item.MouseLeave += (_, _) => item.Background = new SolidColorBrush(Color.FromRgb(42, 42, 48));
            stack.Children.Add(item);
        }
        border.Child = stack;
        Content = border;
        MouseLeftButtonDown += (_, _) => DragMove();
    }
}