using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Swifter.Core.Config;

namespace Swifter.Core.UI;

public sealed class CustomThemeBuilderModal : Window
{
    private readonly Slider _opacitySlider;
    private readonly Slider _fontSizeSlider;
    private readonly ComboBox _backdropCombo;
    private readonly ComboBox _themeCombo;

    public CustomThemeBuilderModal()
    {
        Title = "Customize Theme";
        Width = 480;
        Height = 580;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        _opacitySlider = new Slider();
        _fontSizeSlider = new Slider();
        _backdropCombo = new ComboBox();
        _themeCombo = new ComboBox();
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
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = "🎨 Customize", FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 20) });

        stack.Children.Add(CreateLabel("Theme"));
        _themeCombo.Items.Add("Dark");
        _themeCombo.Items.Add("Light");
        _themeCombo.SelectedIndex = 0;
        _themeCombo.Background = new SolidColorBrush(Color.FromRgb(42, 42, 48));
        _themeCombo.Foreground = Brushes.White;
        _themeCombo.Margin = new Thickness(0, 0, 0, 16);
        stack.Children.Add(_themeCombo);

        stack.Children.Add(CreateLabel("Backdrop Type"));
        _backdropCombo.Items.Add("Mica");
        _backdropCombo.Items.Add("Acrylic");
        _backdropCombo.Items.Add("Mica Alt");
        _backdropCombo.SelectedIndex = 0;
        _backdropCombo.Background = new SolidColorBrush(Color.FromRgb(42, 42, 48));
        _backdropCombo.Foreground = Brushes.White;
        _backdropCombo.Margin = new Thickness(0, 0, 0, 16);
        stack.Children.Add(_backdropCombo);

        stack.Children.Add(CreateLabel("Glass Opacity"));
        _opacitySlider.Minimum = 0;
        _opacitySlider.Maximum = 100;
        _opacitySlider.Value = 85;
        _opacitySlider.TickFrequency = 5;
        _opacitySlider.IsSnapToTickEnabled = true;
        _opacitySlider.Margin = new Thickness(0, 0, 0, 16);
        stack.Children.Add(_opacitySlider);

        stack.Children.Add(CreateLabel("Font Size"));
        _fontSizeSlider.Minimum = 10;
        _fontSizeSlider.Maximum = 24;
        _fontSizeSlider.Value = 14;
        _fontSizeSlider.TickFrequency = 1;
        _fontSizeSlider.IsSnapToTickEnabled = true;
        _fontSizeSlider.Margin = new Thickness(0, 0, 0, 16);
        stack.Children.Add(_fontSizeSlider);

        stack.Children.Add(CreateLabel("Accent Color"));
        var colorPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 16) };
        Color[] colors = { Color.FromRgb(0, 120, 212), Color.FromRgb(0, 153, 76), Color.FromRgb(196, 43, 28), Color.FromRgb(255, 140, 0), Color.FromRgb(136, 43, 216), Color.FromRgb(0, 183, 195), Color.FromRgb(255, 64, 129), Color.FromRgb(100, 100, 100) };
        foreach (var color in colors)
        {
            var btn = new Button
            {
                Width = 32, Height = 32,
                Background = new SolidColorBrush(color),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(2),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = color
            };
            btn.Click += (_, e) => { if (e.Source is Button b && b.Tag is Color c) SettingsManager.Instance.Settings.Appearance.AccentColor = $"#{c.R:X2}{c.G:X2}{c.B:X2}"; };
            colorPanel.Children.Add(btn);
        }
        stack.Children.Add(colorPanel);

        var applyBtn = new Button { Content = "Apply Theme", Height = 36, FontSize = 14, Background = new SolidColorBrush(Color.FromRgb(0, 120, 212)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Margin = new Thickness(0, 8, 0, 8), Cursor = System.Windows.Input.Cursors.Hand };
        applyBtn.Click += (_, _) => ApplyTheme();
        stack.Children.Add(applyBtn);
        var closeBtn = new Button { Content = "Close", Height = 36, FontSize = 14, Background = new SolidColorBrush(Color.FromRgb(50, 50, 56)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
        closeBtn.Click += (_, _) => Close();
        stack.Children.Add(closeBtn);
        scroll.Content = stack;
        border.Child = scroll;
        Content = border;
        MouseLeftButtonDown += (_, _) => DragMove();
    }

    private void ApplyTheme()
    {
        var settings = SettingsManager.Instance.Settings;
        settings.Appearance.GlassOpacity = _opacitySlider.Value / 100.0;
        settings.Appearance.FontSize = (int)_fontSizeSlider.Value;
        settings.Appearance.Theme = _themeCombo.SelectedItem?.ToString() ?? "Dark";
        settings.Appearance.BackdropType = _backdropCombo.SelectedItem?.ToString() ?? "Mica";
        SettingsManager.Instance.Save();
    }

    private static TextBlock CreateLabel(string text) => new() { Text = text, FontSize = 13, FontWeight = FontWeights.Medium, Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 190)), Margin = new Thickness(0, 0, 0, 6) };
}