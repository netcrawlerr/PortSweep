using Avalonia.Controls;

namespace PortSweep.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Intercept close button to hide to tray instead of exiting process
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }
}
