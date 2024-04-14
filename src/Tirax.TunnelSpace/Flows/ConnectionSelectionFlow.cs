using System;
using Serilog;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.Services.Akka;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Flows;

public interface IConnectionSelectionFlow
{
    Aff<ViewModelBase> Create { get; }
}

public sealed class ConnectionSelectionFlow(ILogger logger, IAppMainWindow mainWindow, ISshManager sshManager, ITunnelConfigStorage storage) : IConnectionSelectionFlow
{
    public Aff<ViewModelBase> Create =>
        from allData in storage.All
        from configVms in allData.Map(CreateInfoVm).Sequence()
        let vm = new ConnectionSelectionViewModel(configVms)
        from _1 in storage.Changes.SubscribeEff(ListenStorageChange(vm))
        from _2 in vm.NewConnectionCommand.SubscribeEff(_ => EditConnection())
        select (ViewModelBase)vm;

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
        from _1 in vm.Edit.SubscribeEff(EditConnection)
        from _2 in vm.PlayOrStop.SubscribeEff(isPlaying => logger.LogResult(isPlaying? Stop(vm) : Play(vm)).ToBackground())
        select vm;

    Eff<Unit> EditConnection(TunnelConfig? config = default) =>
        from view in SuccessEff(new TunnelConfigViewModel(config ?? TunnelConfig.CreateSample(Guid.Empty)))
        from _1 in view.Save.SubscribeEff(c => Update(config is null, c).ToBackground())
        from _2 in view.Back.SubscribeEff(_ => mainWindow.CloseCurrentView.Ignore())
        from _3 in view.Delete.SubscribeEff(_ => (from _1 in storage.Delete(view.Config.Id)
                                                  from _2 in mainWindow.CloseCurrentView
                                                  select unit
                                                 ).ToBackground())
        from _4 in mainWindow.PushView(view)
        select unit;

    Aff<Unit> Update(bool isNew, TunnelConfig config) =>
        from _1 in isNew ? storage.Add(config) : storage.Update(config)
        from _2 in mainWindow.CloseCurrentView
        select unit;

    Aff<Unit> Play(ConnectionInfoPanelViewModel vm) =>
        from controller in vm.Controller.Map(SuccessAff).IfNone(() => sshManager.CreateSshController(vm.Config))
        from state in controller.Start
        from _ in Eff(() => vm.Play(controller, state))
        select unit;

    static Eff<Unit> Stop(ConnectionInfoPanelViewModel vm) =>
        from controller in Eff(() => vm.Controller.Get())
        from _1 in eff(controller.Dispose)
        from _2 in Eff(vm.Stop)
        select unit;


}