using System.Collections.ObjectModel;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;
using RUnit = System.Reactive.Unit;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class ConnectionSelectionViewModel : ViewModelBase
{
    public ObservableCollection<TunnelConfig> TunnelConfigs { get; } = new();

    public ReactiveCommand<RUnit, RUnit> NewConnectionCommand { get; } = ReactiveCommand.Create(() => { });
}