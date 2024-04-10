using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Data.Converters;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class TunnelConfigToConnectionInfoPanel : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TunnelConfig tunnelConfig ? new ConnectionInfoPanelViewModel(tunnelConfig) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ConnectionInfoPanelViewModel viewModel ? viewModel.Model : null;
}

public sealed class ConnectionInfoPanelViewModel : ViewModelBase
{
    TunnelConfig tunnelConfig;

    string name = "(Sample name)";

    [DesignOnly(true)]
    public ConnectionInfoPanelViewModel() : this(new(Guid.Empty, "(Sample Host)", 9999, 8888, "(Sample Remote Host)", 7777, "(Sample Name)")) { }

    public ConnectionInfoPanelViewModel(TunnelConfig tunnelConfig)
    {
        this.tunnelConfig = tunnelConfig;
        Name = tunnelConfig.Name;
    }

    public string Name {
        get => name;
        set => this.RaiseAndSetIfChanged(ref name, value);
    }

    public TunnelConfig Model {
        get => tunnelConfig;
        set => this.RaiseAndSetIfChanged(ref tunnelConfig, value);
    }
}