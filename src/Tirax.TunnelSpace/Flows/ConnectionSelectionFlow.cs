using System;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Flows;

public interface IConnectionSelectionFlow
{
    Aff<ViewModelBase> Create { get; }
}

public sealed class ConnectionSelectionFlow(IAppMainWindow mainWindow, ITunnelConfigStorage storage) : IConnectionSelectionFlow
{
    public Aff<ViewModelBase> Create =>
        from allData in storage.All
        let editCommand = AppCommands.CreateEdit()
        let configVms = allData.Map(config => new ConnectionInfoPanelViewModel(editCommand, config)).ToSeq()
        let vm = new ConnectionSelectionViewModel(editCommand, configVms)
        from _1 in storage.Changes.SubscribeEff(ListenStorageChange(editCommand, vm))
        from _2 in vm.NewConnectionCommand.SubscribeEff(_ => EditConnection())
        from _3 in vm.Edit.SubscribeEff(EditConnection)
        select (ViewModelBase)vm;

    static Func<Change<TunnelConfig>, Eff<Unit>> ListenStorageChange(
        ReactiveCommand<TunnelConfig, TunnelConfig> editCommand,
        ConnectionSelectionViewModel vm) =>
        change => change switch
                  { EntryAdded<TunnelConfig> add =>
                        from configVM in SuccessEff(new ConnectionInfoPanelViewModel(editCommand, add.Value))
                        from _ in vm.TunnelConfigs.AddEff(configVM)
                        select unit,

                    EntryMapped<TunnelConfig, TunnelConfig> update =>
                        from configVM in SuccessEff(new ConnectionInfoPanelViewModel(editCommand, update.To))
                        from _ in vm.TunnelConfigs.ReplaceEff(item => item.Config.Id == update.From.Id, configVM)
                        select unit,

                    EntryRemoved<TunnelConfig> delete =>
                        vm.TunnelConfigs.RemoveEff(item => item.Config.Id == delete.OldValue.Id).Ignore(),

                    _ => unitEff };

    Eff<Unit> EditConnection(TunnelConfig? config = default) =>
        from view in SuccessEff(new TunnelConfigViewModel(config ?? TunnelConfig.CreateSample(Guid.Empty)))
        from _1 in view.Save.SubscribeEff(c => Update(config is null, c).ToBackground())
        from _2 in view.Back.SubscribeEff(_ => mainWindow.CloseCurrentView.Ignore())
        from _3 in mainWindow.PushView(view)
        select unit;

    Aff<Unit> Update(bool isNew, TunnelConfig config) =>
        from _1 in isNew ? storage.Add(config) : storage.Update(config)
        from _2 in mainWindow.CloseCurrentView
        select unit;
}