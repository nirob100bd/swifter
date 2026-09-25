using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Swifter.Core.Native;

namespace Swifter.Core.UI;

public sealed class WindowDragEngine
{
    private readonly Window _window;
    private IntPtr _hwnd;
    private bool _isDragging;
    private Point _dragStartPoint;

    public WindowDragEngine(Window window)
    {
        _window = window;
        _window.Loaded += (_, _) =>
        {
            _hwnd = new WindowInteropHelper(_window).Handle;
            var source = HwndSource.FromHwnd(_hwnd);
            source?.AddHook(WndProc);
        };
    }

    public void Drag()
    {
        if (_hwnd == IntPtr.Zero) return;
        Win32Interop.DragMove(_hwnd);
    }

    public void StartResize(ResizeDirection direction)
    {
        if (_hwnd == IntPtr.Zero) return;
        Win32Interop.ReleaseCapture();
        int dir = direction switch
        {
            ResizeDirection.Left => Win32Interop.HTLEFT,
            ResizeDirection.Right => Win32Interop.HTRIGHT,
            ResizeDirection.Top => Win32Interop.HTTOP,
            ResizeDirection.Bottom => Win32Interop.HTBOTTOM,
            ResizeDirection.TopLeft => Win32Interop.HTTOPLEFT,
            ResizeDirection.TopRight => Win32Interop.HTTOPRIGHT,
            ResizeDirection.BottomLeft => Win32Interop.HTBOTTOMLEFT,
            ResizeDirection.BottomRight => Win32Interop.HTBOTTOMRIGHT,
            _ => Win32Interop.HTCLIENT
        };
        Win32Interop.SendMessage(_hwnd, Win32Interop.WM_NCLBUTTONDOWN, (IntPtr)dir, IntPtr.Zero);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case Win32Interop.WM_GETMINMAXINFO:
                var mmi = System.Runtime.InteropServices.Marshal.PtrToStructure<Win32Interop.MINMAXINFO>(lParam);
                var monitor = Win32Interop.MonitorFromPoint(new Win32Interop.POINT { X = (int)_window.Left, Y = (int)_window.Top }, Win32Interop.MONITOR_DEFAULTTONEAREST);
                var monitorInfo = new Win32Interop.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<Win32Interop.MONITORINFO>() };
                Win32Interop.GetMonitorInfo(monitor, ref monitorInfo);
                var work = monitorInfo.rcWork;
                mmi.ptMaxPosition.X = work.Left;
                mmi.ptMaxPosition.Y = work.Top;
                mmi.ptMaxSize.X = work.Right - work.Left;
                mmi.ptMaxSize.Y = work.Bottom - work.Top;
                mmi.ptMinTrackSize.X = (int)_window.MinWidth;
                mmi.ptMinTrackSize.Y = (int)_window.MinHeight;
                System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, lParam, true);
                handled = true;
                break;

            case Win32Interop.WM_NCHITTEST:
                if (_window.WindowState == WindowState.Maximized) break;
                var point = new Point(
                    (short)(lParam.ToInt32() & 0xFFFF),
                    (short)(lParam.ToInt32() >> 16));
                var windowPoint = _window.PointFromScreen(point);
                int border = 6;
                int result = Win32Interop.HTCLIENT;

                if (windowPoint.X < border && windowPoint.Y < border)
                    result = Win32Interop.HTTOPLEFT;
                else if (windowPoint.X > _window.ActualWidth - border && windowPoint.Y < border)
                    result = Win32Interop.HTTOPRIGHT;
                else if (windowPoint.X < border && windowPoint.Y > _window.ActualHeight - border)
                    result = Win32Interop.HTBOTTOMLEFT;
                else if (windowPoint.X > _window.ActualWidth - border && windowPoint.Y > _window.ActualHeight - border)
                    result = Win32Interop.HTBOTTOMRIGHT;
                else if (windowPoint.X < border)
                    result = Win32Interop.HTLEFT;
                else if (windowPoint.X > _window.ActualWidth - border)
                    result = Win32Interop.HTRIGHT;
                else if (windowPoint.Y < border)
                    result = Win32Interop.HTTOP;
                else if (windowPoint.Y > _window.ActualHeight - border)
                    result = Win32Interop.HTBOTTOM;

                if (result != Win32Interop.HTCLIENT)
                {
                    handled = true;
                    return (IntPtr)result;
                }
                break;
        }
        return IntPtr.Zero;
    }
}

public enum ResizeDirection
{
    Left,
    Right,
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}