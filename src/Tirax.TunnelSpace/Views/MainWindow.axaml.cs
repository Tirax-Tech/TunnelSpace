using Avalonia.Controls;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Views;

public partial class MainWindow : Window
{
    public MainWindow() {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
#if DEBUG
        InitializeComponent(attachDevTools: true);
#else
        InitializeComponent();
#endif
        DataContext = viewModel;
    }
}