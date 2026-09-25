using System.Windows;
using Swifter.Core.Native;
using Swifter.Core.UI;

namespace Swifter;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var optimizer = CpuOptimizer.Instance;
        optimizer.SetHighPriorityProcess();
        optimizer.ConfigureThreadPool();
        optimizer.OptimizeNetworkThreads();

        var app = new Application();
        app.ShutdownMode = ShutdownMode.OnMainWindowClosing;
        app.DispatcherUnhandledException += (_, e) =>
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Swifter", "crash.log"),
                $"[{System.DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {e.Exception}\n\n");
            e.Handled = true;
        };
        var mainWindow = new MainWindow();
        app.MainWindow = mainWindow;
        mainWindow.Show();
        app.Run();
    }
}