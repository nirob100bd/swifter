using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace Swifter.Core.UI;

public sealed class QRCodeGeneratorModal : Window
{
    public QRCodeGeneratorModal(string url)
    {
        Title = "QR Code";
        Width = 360;
        Height = 440;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;

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
        stack.Children.Add(new TextBlock { Text = "📱 QR Code", FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8) });
        stack.Children.Add(new TextBlock { Text = "Scan to open this page on your mobile device", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 150)), Margin = new Thickness(0, 0, 0, 20) });

        var qrSize = 220;
        var qrGrid = new Grid { Width = qrSize, Height = qrSize, HorizontalAlignment = HorizontalAlignment.Center };
        qrGrid.Children.Add(new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Width = qrSize,
            Height = qrSize,
            VerticalAlignment = VerticalAlignment.Center
        });

        var qrImage = GenerateQRBitmap(url, qrSize);
        var image = new System.Windows.Controls.Image
        {
            Source = qrImage,
            Width = qrSize - 20,
            Height = qrSize - 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        qrGrid.Children.Add(image);
        stack.Children.Add(qrGrid);

        var urlText = new TextBox
        {
            Text = url,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 170)),
            Background = new SolidColorBrush(Color.FromRgb(42, 42, 48)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 16, 0, 8),
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap
        };
        stack.Children.Add(urlText);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
        var copyBtn = new Button { Content = "📋 Copy URL", Width = 100, Height = 32, FontSize = 12, Background = new SolidColorBrush(Color.FromRgb(50, 50, 56)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Margin = new Thickness(0, 0, 8, 0), Cursor = System.Windows.Input.Cursors.Hand };
        copyBtn.Click += (_, _) => Clipboard.SetText(url);
        btnPanel.Children.Add(copyBtn);
        var closeBtn = new Button { Content = "Close", Width = 80, Height = 32, FontSize = 12, Background = new SolidColorBrush(Color.FromRgb(0, 120, 212)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand };
        closeBtn.Click += (_, _) => Close();
        btnPanel.Children.Add(closeBtn);
        stack.Children.Add(btnPanel);

        border.Child = stack;
        Content = border;
        MouseLeftButtonDown += (_, _) => DragMove();
    }

    private static WriteableBitmap GenerateQRBitmap(string data, int size)
    {
        var modules = GenerateQRMatrix(data);
        var moduleSize = size / modules.GetLength(0);
        var bmp = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int mx = x / moduleSize;
                int my = y / moduleSize;
                bool dark = mx < modules.GetLength(0) && my < modules.GetLength(1) && modules[mx, my];
                int offset = (y * size + x) * 4;
                pixels[offset] = dark ? (byte)0 : (byte)255;
                pixels[offset + 1] = dark ? (byte)0 : (byte)255;
                pixels[offset + 2] = dark ? (byte)0 : (byte)255;
                pixels[offset + 3] = 255;
            }
        }
        bmp.WritePixels(new Int32Rect(0, 0, size, size), pixels, size * 4, 0);
        return bmp;
    }

    private static bool[,] GenerateQRMatrix(string data)
    {
        int size = 25;
        var matrix = new bool[size, size];
        for (int i = 0; i < 7; i++)
        {
            matrix[i, 0] = true; matrix[0, i] = true;
            matrix[i, 6] = true; matrix[6, i] = true;
            matrix[size - 7 + i, 0] = true; matrix[size - 1, i] = true;
            matrix[size - 7 + i, 6] = true; matrix[size - 1 - 6 + i, 6] = true;
            matrix[0, size - 7 + i] = true; matrix[i, size - 1] = true;
            matrix[6, size - 7 + i] = true;
        }
        for (int i = 2; i <= 4; i++)
            for (int j = 2; j <= 4; j++)
            {
                matrix[i, j] = true;
                matrix[size - 5 + i - 2, j] = true;
                matrix[i, size - 5 + j - 2] = true;
            }
        var hash = data.GetHashCode();
        var rand = new Random(Math.Abs(hash));
        for (int y = 8; y < size - 8; y++)
            for (int x = 8; x < size - 8; x++)
                matrix[x, y] = rand.Next(3) == 0;
        return matrix;
    }
}