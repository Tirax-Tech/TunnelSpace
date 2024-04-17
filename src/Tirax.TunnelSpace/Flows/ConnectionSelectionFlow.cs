using System;
using Serilog;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Features.TunnelConfigPage;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.Services.Akka;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Flows;

public interface IConnectionSelectionFlow
{
    Aff<PageModelBase> Create { get; }
}

public sealed class ConnectionSelectionFlow(ILogger logger, IAppMainWindow mainWindow, ISshManager sshManager,
                                            IUniqueId uniqueId) : IConnectionSelectionFlow
{
    public Aff<PageModelBase> Create =>
        from allData in sshManager.All
        from configVms in allData.ToSeq().Map(CreateInfoVm).Sequence()
        let vm = new ConnectionSelectionViewModel(configVms)
        from _1 in sshManager.Changes.SubscribeEff(ListenStorageChange(vm))
        from _2 in vm.NewConnectionCommand.SubscribeEff(_ => EditConnection().ToBackground())
        select (PageModelBase)vm;

    Func<Change<TunnelConfig>, Eff<Unit>> ListenStorageChange(
        ConnectionSelectionViewModel vm) =>
        change => change switch
                  { EntryAdded<TunnelConfig> add =>
                        from configVM in CreateInfoVm(add.Value)
                        from _ in vm.TunnelConfigs.AddEff(configVM)
                        select unit,

                    EntryMapped<TunnelConfig, TunnelConfig> update =>
                        from configVM in CreateInfoVm(update.To)
                        from _ in vm.TunnelConfigs.ReplaceEff(item => item.Config.Id == update.From.Id, configVM)
                        select unit,

                    EntryRemoved<TunnelConfig> delete =>
                        vm.TunnelConfigs.RemoveEff(item => item.Config.Id == delete.OldValue.Id).Ignore(),

                    _ => unitEff };

    Eff<ConnectionInfoPanelViewModel> CreateInfoVm(TunnelConfig config) =>
        from vm in SuccessEff(new ConnectionInfoPanelViewModel(config))
        from _1 in vm.Edit.SubscribeEff(c => EditConnection(c).ToBackground())
        from _2 in vm.PlayOrStop.SubscribeEff(isPlaying => logger.LogResult(isPlaying? Stop(vm) : Play(vm)).ToBackground())
        select vm;

    Aff<Unit> EditConnection(Option<TunnelConfig> config = default) =>
        from view in SuccessEff(new TunnelConfigViewModel(config.IfNone(TunnelConfig.CreateSample)))
        from _1 in view.Save.SubscribeEff(c => Update(c).ToBackground())
        from _2 in view.Back.SubscribeEff(_ => mainWindow.CloseCurrentView.ToBackground())
        from _3 in view.Delete.SubscribeEff(_ => (from _1 in sshManager.DeleteTunnel(view.Config.Id!.Value)
                                                  from _2 in mainWindow.CloseCurrentView
                                                  select unit
                                                 ).ToBackground())
        from _4 in mainWindow.PushView(view)
        select unit;

    Aff<Unit> Update(TunnelConfig config) =>
        from _1 in config.Id is null
                       ? from id in uniqueId.NewGuid
                         from ret in sshManager.AddTunnel(config with { Id = id })
                         select ret
                       : sshManager.UpdateTunnel(config)
        from _2 in mainWindow.CloseCurrentView
        select unit;

    Aff<Unit> Play(ConnectionInfoPanelViewModel vm) =>
        from _1 in sshManager.StartTunnel(vm.Config.Id!.Value)
        from _ in Eff(() => vm.SetIsPlaying(state))
        select unit;

    static Eff<Unit> Stop(ConnectionInfoPanelViewModel vm) =>
        from controller in Eff(() => vm.Controller.Get())
        from _1 in eff(controller.Dispose)
        from _2 in Eff(vm.Stop)
        select unit;


}