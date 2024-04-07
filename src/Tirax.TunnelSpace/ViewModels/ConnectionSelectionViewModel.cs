using System.Collections.ObjectModel;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;
using Seq = LanguageExt.Seq;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class ConnectionSelectionViewModel(Seq<TunnelConfig> init) : ViewModelBase
{
    public ConnectionSelectionViewModel() : this(Seq.empty<TunnelConfig>()) { }

    public ObservableCollection<TunnelConfig> TunnelConfigs { get; } = new(init);

    public ReactiveCommand<RUnit, RUnit> NewConnectionCommand { get; } = ReactiveCommand.Create(() => { });
}