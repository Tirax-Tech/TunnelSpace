using Avalonia.Controls;

namespace Tirax.TunnelSpace.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
#if DEBUG
        InitializeComponent(attachDevTools: true);
#else
        InitializeComponent();
#endif
    }
}