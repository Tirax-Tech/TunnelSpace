using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.Flows;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class ConnectionSelectionViewModel(
    ReactiveCommand<TunnelConfig,TunnelConfig> editCommand,
    Seq<ConnectionInfoPanelViewModel> init) : ViewModelBase
{
    [DesignOnly(true)]
    public ConnectionSelectionViewModel() : this(
        AppCommands.EditDummy,
        Seq<ConnectionInfoPanelViewModel>(
        new(AppCommands.EditDummy,TunnelConfig.CreateSample(Guid.NewGuid())),
        new(AppCommands.EditDummy,TunnelConfig.CreateSample(Guid.NewGuid())),
        new(AppCommands.EditDummy,TunnelConfig.CreateSample(Guid.NewGuid()))))
    { }

    public ObservableCollection<ConnectionInfoPanelViewModel> TunnelConfigs { get; } = new(init);

    public ReactiveCommand<Unit, Unit> NewConnectionCommand { get; } = ReactiveCommand.Create<Unit,Unit>(_ => unit);

    public ReactiveCommand<TunnelConfig, TunnelConfig> Edit { get; } = editCommand;
}