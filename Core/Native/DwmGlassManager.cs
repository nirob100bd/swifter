using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Swifter.Core.Native;

public sealed class DwmGlassManager
{
    private readonly Window _window;
    private IntPtr _hwnd;
    private bool _isGlassExtended;
    private double _glassOpacity = 0.8;
    private Color _tintColor = Color.FromArgb(180, 0, 0, 0);

    public double GlassOpacity
    {
        get => _glassOpacity;
        set
        {
            _glassOpacity = Math.Clamp(value, 0.0, 1.0);
            UpdateGlassAppearance();
        }
    }

    public Color TintColor
    {
        get => _tintColor;
        set
        {
            _tintColor = value;
            UpdateGlassAppearance();
        }
    }

    public bool IsGlassExtended => _isGlassExtended;

    public DwmGlassManager(Window window)
    {
        _window = window;
        _window.Loaded += OnWindowLoaded;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        _hwnd = new WindowInteropHelper(_window).Handle;
        Win32Interop.SetRoundedCorners(_hwnd, true);
        ExtendGlass();
    }

    public void ExtendGlass()
    {
        if (_hwnd == IntPtr.Zero) return;
        Win32Interop.ExtendGlassIntoClient(_window);
        _isGlassExtended = true;
    }

    public void SetBackdropType(BackdropType type)
    {
        if (_hwnd == IntPtr.Zero) return;
        int attr = type switch
        {
            BackdropType.Mica => 2,
            BackdropType.Acrylic => 3,
            BackdropType.MicaAlt => 4,
            _ => 0
        };
        Win32Interop.DwmSetWindowAttribute(_hwnd, 38, ref attr, sizeof(int));
    }

    public void EnableShadow(bool enable)
    {
        if (_hwnd == IntPtr.Zero) return;
        int val = enable ? 2 : 0;
        Win32Interop.DwmSetWindowAttribute(_hwnd, 2, ref val, sizeof(int));
    }

    public void SetBorderColor(Color color)
    {
        if (_hwnd == IntPtr.Zero) return;
        int colorRef = color.R | (color.G << 8) | (color.B << 16);
        Win32Interop.DwmSetWindowAttribute(_hwnd, 34, ref colorRef, sizeof(int));
    }

    private void UpdateGlassAppearance()
    {
        if (_hwnd == IntPtr.Zero) return;
        var margins = new Win32Interop.RECT
        {
            Left = -1,
            Top = -1,
            Right = -1,
            Bottom = -1
        };
        Win32Interop.DwmExtendFrameIntoClientArea(_hwnd, ref margins);
    }

    public void RemoveGlass()
    {
        if (_hwnd == IntPtr.Zero) return;
        var margins = new Win32Interop.RECT
        {
            Left = 0,
            Top = 0,
            Right = 0,
            Bottom = 0
        };
        Win32Interop.DwmExtendFrameIntoClientArea(_hwnd, ref margins);
        _isGlassExtended = false;
    }

    public void SetCaptionColor(Color color)
    {
        if (_hwnd == IntPtr.Zero) return;
        int colorRef = color.R | (color.G << 8) | (color.B << 16);
        Win32Interop.DwmSetWindowAttribute(_hwnd, 35, ref colorRef, sizeof(int));
    }

    public void Dispose()
    {
        _window.Loaded -= OnWindowLoaded;
        RemoveGlass();
    }
}

public enum BackdropType
{
    None,
    Mica,
    Acrylic,
    MicaAlt
}