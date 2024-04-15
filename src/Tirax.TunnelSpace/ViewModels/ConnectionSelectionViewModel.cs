using System.Collections.ObjectModel;
using System.ComponentModel;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class ConnectionSelectionViewModel(Seq<ConnectionInfoPanelViewModel> init) : PageModelBase(new SearchHeaderViewModel())
{
    [DesignOnly(true)]
    public ConnectionSelectionViewModel() : this(Seq<ConnectionInfoPanelViewModel>(new(TunnelConfig.CreateSample()),
                                                                                   new(TunnelConfig.CreateSample()),
                                                                                   new(TunnelConfig.CreateSample()))) {
    }

    public ObservableCollection<ConnectionInfoPanelViewModel> TunnelConfigs { get; } = new(init);

    public ReactiveCommand<Unit, Unit> NewConnectionCommand { get; } = ReactiveCommand.Create<Unit,Unit>(_ => unit);
}