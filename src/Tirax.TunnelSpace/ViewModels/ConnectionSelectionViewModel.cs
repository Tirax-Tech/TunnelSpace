using System.Collections.ObjectModel;
using System.ComponentModel;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class ConnectionSelectionViewModel(Seq<TunnelConfig> init) : ViewModelBase
{
    [DesignOnly(true)]
    public ConnectionSelectionViewModel() : this(Seq(TunnelConfig.Sample,
                                                     TunnelConfig.Sample,
                                                     TunnelConfig.Sample)) { }

    public ObservableCollection<TunnelConfig> TunnelConfigs { get; } = new(init);

    public ReactiveCommand<RUnit, RUnit> NewConnectionCommand { get; } = ReactiveCommand.Create(() => { });
}