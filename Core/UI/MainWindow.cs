using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Web.WebView2.Wpf;
using Swifter.Core.Native;
using Swifter.Core.TabEngine;
using Swifter.Core.Config;
using Swifter.Core.Network;
using Swifter.Core.Protocols;

namespace Swifter.Core.UI;

public sealed class MainWindow : Window
{
    private readonly TabStripControl _tabStrip;
    private readonly AddressBarControl _addressBar;
    private readonly Grid _webViewContainer;
    private readonly Dictionary<int, WebView2> _webViews = new();
    private readonly DwmGlassManager _glassManager;
    private readonly WindowDragEngine _dragEngine;
    private readonly StatusBarItem _statusBar;
    private WebView2? _activeWebView;

    public MainWindow()
    {
        CpuOptimizer.Instance.SetHighPriorityProcess();
        CpuOptimizer.Instance.ConfigureThreadPool();
        ShieldEngine.Instance.Initialize();
        CpuOptimizer.Instance.OptimizeNetworkThreads();

        Title = "Swifter";
        Width = 1400;
        Height = 900;
        MinWidth = 800;
        MinHeight = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = false;
        Background = new SolidColorBrush(Color.FromRgb(28, 28, 32));

        _glassManager = new DwmGlassManager(this);
        _dragEngine = new WindowDragEngine(this);

        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleBar = CreateTitleBar();
        Grid.SetRow(titleBar, 0);
        mainGrid.Children.Add(titleBar);

        _tabStrip = new TabStripControl();
        _tabStrip.TabCreated += OnTabCreated;
        _tabStrip.TabClosed += OnTabClosed;
        _tabStrip.TabSelected += OnTabSelected;
        Grid.SetRow(_tabStrip, 1);
        mainGrid.Children.Add(_tabStrip);

        _webViewContainer = new Grid { ClipToBounds = true };
        Grid.SetRow(_webViewContainer, 2);
        mainGrid.Children.Add(_webViewContainer);

        _addressBar = new AddressBarControl();
        _addressBar.NavigationRequested += OnNavigationRequested;
        Grid.SetRow(_addressBar, 0);

        var statusBar = CreateStatusBar();
        Grid.SetRow(statusBar, 3);
        mainGrid.Children.Add(statusBar);

        Content = mainGrid;

        Loaded += OnMainWindowLoaded;
        Closing += OnMainWindowClosing;
        StateChanged += OnStateChanged;
        PreviewKeyDown += OnPreviewKeyDown;

        SessionRestorer.Instance.StartAutoSave(30);
        MemorySaver.Instance.StartMonitoring(30);
        TabFreezingEngine.Instance.StartMonitoring(60);
        HibernateScheduleEngine.Instance.StartScheduler(30);
        DownloadQueueManager.Instance.TaskCompleted += (_, task) =>
        {
            Dispatcher.Invoke(() => _statusBar.Text = $"Download complete: {task.FileName}");
        };

        TabManager.Instance.CreateTab();
    }

    private Border CreateTitleBar()
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(28, 28, 32)),
            Height = 36,
            Padding = new Thickness(8, 0, 8, 0)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var logoPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var logoText = new TextBlock
        {
            Text = "⚡",
            FontSize = 16,
            Margin = new Thickness(8, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        logoPanel.Children.Add(logoText);
        Grid.SetColumn(logoPanel, 0);
        grid.Children.Add(logoPanel);

        Grid.SetColumn(_addressBar, 1);
        grid.Children.Add(_addressBar);

        var windowButtons = CreateWindowButtons();
        Grid.SetColumn(windowButtons, 2);
        grid.Children.Add(windowButtons);

        border.Child = grid;
        border.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else
            {
                _dragEngine.Drag();
            }
        };
        return border;
    }

    private StackPanel CreateWindowButtons()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var minimizeBtn = CreateWindowButton("─");
        minimizeBtn.Click += (_, _) => WindowState = WindowState.Minimized;
        panel.Children.Add(minimizeBtn);

        var maximizeBtn = CreateWindowButton("□");
        maximizeBtn.Click += (_, _) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        panel.Children.Add(maximizeBtn);

        var closeBtn = CreateWindowButton("✕");
        closeBtn.Background = new SolidColorBrush(Color.FromRgb(196, 43, 28));
        closeBtn.Click += (_, _) => Close();
        panel.Children.Add(closeBtn);

        return panel;
    }

    private static Button CreateWindowButton(string text)
    {
        return new Button
        {
            Content = text,
            Width = 46,
            Height = 36,
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 190)),
            FontSize = 12,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
    }

    private TextBlock CreateStatusBar()
    {
        _statusBar = new TextBlock
        {
            Text = "Ready",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 130)),
            Padding = new Thickness(12, 6, 12, 6),
            Background = new SolidColorBrush(Color.FromRgb(24, 24, 28))
        };
        return _statusBar;
    }

    private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        Win32Interop.EnableBorderlessWindow(this);
        _glassManager.SetBackdropType(BackdropType.Mica);

        var settings = SettingsManager.Instance.Settings;
        if (settings.General.RestoreTabsOnStart)
        {
            var session = SessionRestorer.Instance.RestoreLastSession();
            if (session != null && session.Count > 0)
            {
                foreach (var tab in session)
                {
                    var newTab = TabManager.Instance.CreateTab(tab.Url, tab.Title, tab.IsActive);
                }
            }
        }

        var schemeHandler = new SwifterSchemeHandler();
        var profile = SandboxManager.Instance.GetProfile("default");
        if (profile != null)
        {
            schemeHandler.RegisterProtocol();
        }
    }

    private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SessionRestorer.Instance.SaveSnapshot();
        SessionRestorer.Instance.StopAutoSave();
        MemorySaver.Instance.StopMonitoring();
        TabFreezingEngine.Instance.StopMonitoring();
        HibernateScheduleEngine.Instance.StopScheduler();

        foreach (var kv in _webViews)
        {
            try { kv.Value.Dispose(); } catch { }
        }
        _webViews.Clear();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
    }

    private async void OnTabCreated(object? sender, TabData tab)
    {
        var webView = await CreateWebViewAsync();
        _webViews[tab.Id] = webView;
        _webViewContainer.Children.Add(webView);

        if (tab.Url != "swifter://newtab")
        {
            webView.CoreWebView2.Navigate(tab.Url);
        }
        else
        {
            webView.CoreWebView2.Navigate("swifter://newtab");
        }

        webView.CoreWebView2.NavigationCompleted += (_, args) =>
        {
            Dispatcher.Invoke(() =>
            {
                tab.Title = webView.CoreWebView2.DocumentTitle;
                tab.IsLoading = false;
                _statusBar.Text = $"Navigation complete: {tab.Title}";
            });
        };

        webView.CoreWebView2.SourceChanged += (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                tab.Url = webView.CoreWebView2.Source;
                _addressBar.UpdateUrl(tab.Url);
            });
        };

        webView.CoreWebView2.NewWindowRequested += (_, args) =>
        {
            args.Handled = true;
            var newTab = TabManager.Instance.CreateTab(args.Uri);
        };

        webView.CoreWebView2.DownloadStarting += (_, args) =>
        {
            var fileName = args.ResultFilePath;
            _statusBar.Text = $"Download starting: {Path.GetFileName(fileName)}";
        };

        UpdateWebViewVisibility();
    }

    private void OnTabClosed(object? sender, TabData tab)
    {
        if (_webViews.TryGetValue(tab.Id, out var webView))
        {
            _webViewContainer.Children.Remove(webView);
            webView.Dispose();
            _webViews.Remove(tab.Id);
        }
        UpdateWebViewVisibility();
    }

    private void OnTabSelected(object? sender, TabData tab)
    {
        TabManager.Instance.SetActive(tab.Id);
        UpdateWebViewVisibility();
        _addressBar.UpdateUrl(tab.Url);
    }

    private void UpdateWebViewVisibility()
    {
        var active = TabManager.Instance.ActiveTab;
        foreach (var kv in _webViews)
        {
            kv.Value.Visibility = active != null && kv.Key == active.Id ? Visibility.Visible : Visibility.Collapsed;
        }
        if (active != null && _webViews.TryGetValue(active.Id, out var webView))
        {
            _activeWebView = webView;
        }
    }

    private async Task<WebView2> CreateWebViewAsync()
    {
        var webView = new WebView2();
        var profileDir = SandboxManager.Instance.GetProfileDataDir("default");
        var env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(profileDir, "WebView2"));
        await webView.EnsureCoreWebView2Async(env);
        webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
        webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        webView.CoreWebView2.Settings.IsSwipeNavigationEnabled = true;
        webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

        webView.CoreWebView2.WebResourceRequested += (_, args) =>
        {
            var url = args.Request.Uri;
            if (ShieldEngine.Instance.ShouldBlock(url))
            {
                args.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(null, 403, "Blocked", "");
            }
            MediaSniffer.Instance.AnalyzeRequest(url);
        };

        return webView;
    }

    private void OnNavigationRequested(object? sender, string url)
    {
        if (_activeWebView == null) return;
        if (!url.StartsWith("http") && !url.StartsWith("swifter://") && !url.StartsWith("ftp://"))
        {
            if (url.Contains('.') && !url.Contains(' '))
            {
                url = "https://" + url;
            }
            else
            {
                url = SettingsManager.Instance.Settings.General.DefaultSearchEngine + Uri.EscapeDataString(url);
            }
        }
        _activeWebView.CoreWebView2.Navigate(url);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.T:
                    TabManager.Instance.CreateTab();
                    e.Handled = true;
                    break;
                case Key.W:
                    var active = TabManager.Instance.ActiveTab;
                    if (active != null) TabManager.Instance.CloseTab(active.Id);
                    e.Handled = true;
                    break;
                case Key.L:
                    _addressBar.FocusAddressBar();
                    e.Handled = true;
                    break;
                case Key.R:
                    _activeWebView?.CoreWebView2?.Reload();
                    e.Handled = true;
                    break;
                case Key.D:
                    _activeWebView?.CoreWebView2?.ExecuteScriptAsync("window.print()");
                    e.Handled = true;
                    break;
                case Key.Tab:
                    TabManager.Instance.SetActive(
                        TabManager.Instance.Tabs.SkipWhile(t => !t.IsActive).Skip(1).FirstOrDefault()?.Id ??
                        TabManager.Instance.Tabs.FirstOrDefault()?.Id ?? 0);
                    UpdateWebViewVisibility();
                    e.Handled = true;
                    break;
            }
        }
        if (e.Key == Key.F5)
        {
            _activeWebView?.CoreWebView2?.Reload();
            e.Handled = true;
        }
        if (e.Key == Key.F12)
        {
            _activeWebView?.CoreWebView2?.OpenDevTools();
            e.Handled = true;
        }
        if (e.Key == Key.F11)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            e.Handled = true;
        }
    }
}