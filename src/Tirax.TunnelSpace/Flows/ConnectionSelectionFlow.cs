using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Flows;

public sealed class ConnectionSelectionFlow(ITunnelConfigStorage storage)
{
    public Aff<ConnectionSelectionViewModel> Create =>
        from allData in storage.All
        let vm = new ConnectionSelectionViewModel(allData)
        from _1 in storage.Changes.SubscribeEff(change => change switch
        {
            EntryAdded<TunnelConfig> add => vm.TunnelConfigs.AddEff(add.Value),
            EntryMapped<TunnelConfig, TunnelConfig> update => vm.TunnelConfigs.ReplaceEff(update.From, update.To).Ignore(),
            EntryRemoved<TunnelConfig> delete => vm.TunnelConfigs.RemoveEff(delete.OldValue).Ignore(),
            _ => unitEff
        })
        select vm;
}