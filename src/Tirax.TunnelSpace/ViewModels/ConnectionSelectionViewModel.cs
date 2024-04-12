using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class ConnectionSelectionViewModel(Seq<ConnectionInfoPanelViewModel> init) : ViewModelBase
{
    [DesignOnly(true)]
    public ConnectionSelectionViewModel() : this(
        Seq<ConnectionInfoPanelViewModel>(
        new(TunnelConfig.CreateSample(Guid.NewGuid())),
        new(TunnelConfig.CreateSample(Guid.NewGuid())),
        new(TunnelConfig.CreateSample(Guid.NewGuid()))))
    { }

    public ObservableCollection<ConnectionInfoPanelViewModel> TunnelConfigs { get; } = new(init);

    public ReactiveCommand<Unit, Unit> NewConnectionCommand { get; } = ReactiveCommand.Create<Unit,Unit>(_ => unit);
}