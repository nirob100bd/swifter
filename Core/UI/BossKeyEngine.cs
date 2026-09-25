using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Swifter.Core.UI;

public sealed class BossKeyEngine : IDisposable
{
    private static BossKeyEngine? _instance;
    private Window? _overlayWindow;
    private bool _isHidden;
    private IntPtr _hookId = IntPtr.Zero;
    private readonly NativeMethods.LowLevelKeyboardProc _hookDelegate;

    public static BossKeyEngine Instance => _instance ??= new BossKeyEngine();

    public bool IsHidden => _isHidden;
    public KeyGesture TriggerGesture { get; set; } = new(Key.Z, ModifierKeys.Windows);

    public event EventHandler<bool>? BossModeChanged;

    private BossKeyEngine()
    {
        _hookDelegate = KeyboardHookCallback;
    }

    public void RegisterGlobalHotkey(Window mainWindow)
    {
        _hookId = SetKeyboardHook(_hookDelegate);
    }

    public void UnregisterGlobalHotkey()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)0x0100)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            bool isWin = (NativeMethods.GetAsyncKeyState(0x5B) & 0x8000) != 0 || (NativeMethods.GetAsyncKeyState(0x5C) & 0x8000) != 0;
            if (isWin && vkCode == 0x5A)
            {
                Application.Current.Dispatcher.Invoke(ToggleBossMode);
                return (IntPtr)1;
            }
        }
        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void ToggleBossMode()
    {
        if (_isHidden)
            DeactivateBossMode();
        else
            ActivateBossMode();
    }

    private void ActivateBossMode()
    {
        if (Application.Current.MainWindow != null)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
            Application.Current.MainWindow.ShowInTaskbar = false;
            Application.Current.MainWindow.Hide();
        }
        _overlayWindow = new Window
        {
            Title = "Microsoft Excel - Budget_Q4.xlsx",
            Width = 1200,
            Height = 800,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            WindowState = WindowState.Maximized,
            Content = CreateExcelOverlay()
        };
        _overlayWindow.Show();
        _isHidden = true;
        BossModeChanged?.Invoke(this, true);
    }

    private void DeactivateBossMode()
    {
        _overlayWindow?.Close();
        _overlayWindow = null;
        if (Application.Current.MainWindow != null)
        {
            Application.Current.MainWindow.ShowInTaskbar = true;
            Application.Current.MainWindow.Show();
            Application.Current.MainWindow.WindowState = WindowState.Maximized;
        }
        _isHidden = false;
        BossModeChanged?.Invoke(this, false);
    }

    private static System.Windows.Controls.Grid CreateExcelOverlay()
    {
        var grid = new System.Windows.Controls.Grid();
        for (int i = 0; i < 26; i++)
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < 40; i++)
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(24) });
        var rand = new Random();
        for (int r = 0; r < 40; r++)
        {
            for (int c = 0; c < 26; c++)
            {
                var cell = new System.Windows.Controls.Border
                {
                    BorderBrush = System.Windows.Media.Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 0.5, 0.5)
                };
                var text = new System.Windows.Controls.TextBlock
                {
                    Text = (r == 0) ? ((char)('A' + c)).ToString() : rand.Next(100, 99999).ToString("N0"),
                    FontSize = 11,
                    FontFamily = new System.Windows.Media.FontFamily("Calibri"),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(4, 0, 4, 0)
                };
                if (r == 0) { text.FontWeight = FontWeights.Bold; text.HorizontalAlignment = HorizontalAlignment.Center; }
                cell.Child = text;
                System.Windows.Controls.Grid.SetRow(cell, r);
                System.Windows.Controls.Grid.SetColumn(cell, c);
                grid.Children.Add(cell);
            }
        }
        return grid;
    }

    private static IntPtr SetKeyboardHook(NativeMethods.LowLevelKeyboardProc proc)
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return NativeMethods.SetWindowsHookEx(13, proc, NativeMethods.GetModuleHandle(curModule?.ModuleName), 0);
    }

    public void Dispose()
    {
        UnregisterGlobalHotkey();
        _overlayWindow?.Close();
    }

    private static class NativeMethods
    {
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
    }
}