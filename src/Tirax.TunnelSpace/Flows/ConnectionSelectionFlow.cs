using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Flows;

public sealed class ConnectionSelectionFlow(ITunnelConfigStorage storage)
{
    public Aff<ConnectionSelectionViewModel> Create =>
        from allData in storage.All
        let editCommand = AppCommands.CreateEdit()
        let configVms = allData.Map(config => new ConnectionInfoPanelViewModel(editCommand, config)).ToSeq()
        let vm = new ConnectionSelectionViewModel(editCommand, configVms)
        from _1 in storage.Changes.SubscribeEff(change => change switch
        {
            EntryAdded<TunnelConfig> add =>
                from configVM in SuccessEff(new ConnectionInfoPanelViewModel(editCommand, add.Value))
                from _ in vm.TunnelConfigs.AddEff(configVM)
                select unit,

            EntryMapped<TunnelConfig, TunnelConfig> update =>
                from configVM in SuccessEff(new ConnectionInfoPanelViewModel(editCommand, update.To))
                from _ in vm.TunnelConfigs.ReplaceEff(item => item.Config.Id == update.From.Id, configVM)
                select unit,

            EntryRemoved<TunnelConfig> delete =>
                vm.TunnelConfigs.RemoveEff(item => item.Config.Id == delete.OldValue.Id).Ignore(),

            _ => unitEff
        })
        select vm;
}