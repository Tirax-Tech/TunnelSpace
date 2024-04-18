using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Serilog;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Features.TunnelConfigPage;
using Tirax.TunnelSpace.Services.Akka;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Flows;

public interface IConnectionSelectionFlow
{
    EitherAsync<Error, PageModelBase> Create();
}

public sealed class ConnectionSelectionFlow(ILogger logger, IAppMainWindow mainWindow, ISshManager sshManager,
                                            IUniqueId uniqueId) : IConnectionSelectionFlow
{
    public EitherAsync<Error, PageModelBase> Create() =>
        from allData in sshManager.RetrieveState()
        let configVms = allData.Map(i => CreateInfoVm(i.Config, i.IsRunning).Run().ThrowIfFail())
        let vm = new ConnectionSelectionViewModel(configVms)
        let _1 = sshManager.Changes.Subscribe(x => ListenStorageChange(vm)(x).RunUnit())
        let _2 = vm.NewConnectionCommand.Subscribe(_ => (EditConnection() | @catch(LogError("new connection"))).RunIgnore())
        select (PageModelBase)vm;

    Func<Change<TunnelConfig>, Eff<Unit>> ListenStorageChange(ConnectionSelectionViewModel vm) =>
        change => change switch
                  {
                      EntryAdded<TunnelConfig> add =>
                          from configVM in CreateInfoVm(add.Value)
                          from _ in vm.TunnelConfigs.AddEff(configVM)
                          select unit,

                      EntryMapped<TunnelConfig, TunnelConfig> update =>
                          from configVM in CreateInfoVm(update.To)
                          from _ in vm.TunnelConfigs.ReplaceEff(item => item.Config.Id == update.From.Id, configVM)
                          select unit,

                      EntryRemoved<TunnelConfig> delete =>
                          vm.TunnelConfigs.RemoveEff(item => item.Config.Id == delete.OldValue.Id).Ignore(),

                      _ => FailEff<Unit>((AppStandardErrors.UnexpectedCode, $"Unrecognized change: {change}"))
                  }
                | @catch(LogError("listening storage changes"));

    Eff<ConnectionInfoPanelViewModel> CreateInfoVm(TunnelConfig config, bool initialPlaying = default) =>
        from vm in SuccessEff(new ConnectionInfoPanelViewModel(config))
        from _1 in vm.Edit.SubscribeEff(c => EditConnection(c) | @catch(LogError("editing connection")))
        from _2 in vm.PlayOrStop.SubscribeEff(isPlaying => logger.LogResult(isPlaying ? Stop(vm) : Play(vm))
                                                         | @catch(LogError("play or stop")))
        let isPlaying = sshManager.TunnelRunningStateChanges
                                  .Where(state => state.Config.Id == vm.Config.Id)
                                  .Select(state => state.IsRunning)
                                  .StartWith(initialPlaying)
        from _3 in vm.SetIsPlaying(isPlaying)
        select vm;

    Aff<Unit> EditConnection(Option<TunnelConfig> config = default) =>
        from view in SuccessEff(new TunnelConfigViewModel(config.IfNone(TunnelConfig.CreateSample)))
        from _1 in view.Save.SubscribeEff(c => Update(c) | @catch(LogError("saving tunnel config")))
        from _2 in view.Back.SubscribeEff(_ => mainWindow.CloseCurrentView | @catch(LogError("back to main view")))
        from _3 in view.Delete.SubscribeEff(_ => (from _1 in sshManager.DeleteTunnel(view.Config.Id!.Value)
                                                  from _2 in mainWindow.CloseCurrentView
                                                  select unit
                                                 ) | @catch(LogError("deleting tunnel")))
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Aff<Unit> Play(ConnectionInfoPanelViewModel vm) =>
        sshManager.StartTunnel(vm.Config.Id!.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Aff<Unit> Stop(ConnectionInfoPanelViewModel vm) =>
        sshManager.StopTunnel(vm.Config.Id!.Value);

    Func<Error, Eff<Unit>> LogError(string action) =>
        e => logger.ErrorEff(e, "Error while {Action}", action);
}