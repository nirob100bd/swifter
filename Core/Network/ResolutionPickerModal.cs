using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Swifter.Core.Network;

public sealed class ResolutionPickerModal : Window
{
    private readonly MediaSource _source;
    private string _selectedResolution = "1080p";

    public string SelectedResolution => _selectedResolution;
    public bool DownloadRequested { get; private set; }

    public ResolutionPickerModal(MediaSource source)
    {
        _source = source;
        _selectedResolution = source.Resolutions.FirstOrDefault() ?? "1080p";
        ConfigureWindow();
        BuildUI();
    }

    private void ConfigureWindow()
    {
        Title = "Download Media";
        Width = 420;
        Height = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
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

        var titleBlock = new TextBlock
        {
            Text = "Download Media",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 8)
        };
        stack.Children.Add(titleBlock);

        var urlBlock = new TextBlock
        {
            Text = _source.Url,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 170)),
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, 0, 16)
        };
        stack.Children.Add(urlBlock);

        var typeBlock = new TextBlock
        {
            Text = $"Type: {_source.Type}",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(120, 200, 255)),
            Margin = new Thickness(0, 0, 0, 16)
        };
        stack.Children.Add(typeBlock);

        var resLabel = new TextBlock
        {
            Text = "Select Quality:",
            FontSize = 14,
            FontWeight = FontWeights.Medium,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 8)
        };
        stack.Children.Add(resLabel);

        var resGroup = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
        foreach (var res in _source.Resolutions)
        {
            var rb = new RadioButton
            {
                Content = res,
                Tag = res,
                Foreground = Brushes.White,
                FontSize = 14,
                Margin = new Thickness(0, 4, 0, 4),
                GroupName = "Resolution",
                IsChecked = res == _selectedResolution
            };
            rb.Checked += (_, _) => _selectedResolution = res;
            resGroup.Children.Add(rb);
        }
        if (_source.Type == MediaType.Audio || _source.Type == MediaType.HLSStream)
        {
            var audioRb = new RadioButton
            {
                Content = "Audio Only (M4A)",
                Tag = "audio",
                Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 80)),
                FontSize = 14,
                Margin = new Thickness(0, 4, 0, 4),
                GroupName = "Resolution"
            };
            audioRb.Checked += (_, _) => _selectedResolution = "audio";
            resGroup.Children.Add(audioRb);
        }
        stack.Children.Add(resGroup);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var cancelBtn = CreateButton("Cancel", Color.FromRgb(60, 60, 66));
        cancelBtn.Click += (_, _) => { DialogResult = false; Close(); };
        btnPanel.Children.Add(cancelBtn);

        var downloadBtn = CreateButton("Download", Color.FromRgb(0, 120, 212));
        downloadBtn.Click += (_, _) => { DownloadRequested = true; DialogResult = true; Close(); };
        btnPanel.Children.Add(downloadBtn);

        stack.Children.Add(btnPanel);
        border.Child = stack;
        Content = border;

        MouseLeftButtonDown += (_, _) => DragMove();
    }

    private static Button CreateButton(string text, Color bg)
    {
        return new Button
        {
            Content = text,
            Width = 100,
            Height = 36,
            Margin = new Thickness(8, 0, 0, 0),
            Background = new SolidColorBrush(bg),
            Foreground = Brushes.White,
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
    }
}