using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PortSweep.Desktop.ViewModels;
using PortSweep.Desktop.Views;

namespace PortSweep.Desktop;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Wire up tray icon left-click
            var icons = TrayIcon.GetIcons(this);
            if (icons != null && icons.Count > 0)
            {
                icons[0].Clicked += OnTrayIconClicked;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnTrayIconClicked(object? sender, EventArgs e) => RestoreMainWindow();

    private void OnOpenWindowClicked(object? sender, EventArgs e) => RestoreMainWindow();

    private void RestoreMainWindow()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var win = desktop.MainWindow;

            if (win == null)
            {
                win = new MainWindow
                {
                    DataContext = new MainViewModel()
                };
                desktop.MainWindow = win;
            }

            win.Show();
            if (win.WindowState == WindowState.Minimized)
            {
                win.WindowState = WindowState.Normal;
            }
            win.Activate();
            win.Focus();
        }
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow?.DataContext is MainViewModel vm)
        {
            await vm.RefreshAsync();
        }
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}