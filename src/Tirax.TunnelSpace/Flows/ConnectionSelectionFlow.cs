using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Serilog;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.Features.TunnelConfigPage;
using Tirax.TunnelSpace.Helpers;
using Tirax.TunnelSpace.Services.Akka;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Flows;

public interface IConnectionSelectionFlow
{
    Aff<PageModelBase> Create();
}

public sealed class ConnectionSelectionFlow(ILogger logger, IAppMainWindow mainWindow, ISshManager sshManager,
                                            IUniqueId uniqueId) : IConnectionSelectionFlow
{
    public Aff<PageModelBase> Create() {
        return sshManager.RetrieveState().Map(create);

        PageModelBase create(Seq<TunnelState> allData) {
            var configVms = allData.Map(i => CreateInfoVm(i.Config, i.IsRunning));
            var vm = new ConnectionSelectionViewModel(configVms);
            sshManager.Changes.Subscribe(ListenStorageChange(vm));
            vm.NewConnectionCommand.Subscribe(_ => EditConnection());
            return vm;
        }
    }

    Action<Change<TunnelConfig>> ListenStorageChange(ConnectionSelectionViewModel vm) =>
        change => {
            switch (change) {
                case EntryAdded<TunnelConfig> add:
                    vm.TunnelConfigs.Add(CreateInfoVm(add.Value));
                    break;

                case EntryMapped<TunnelConfig, TunnelConfig> update:
                    var result = vm.TunnelConfigs.ReplaceFirst(item => item.Config.Id == update.From.Id, CreateInfoVm(update.To));
                    if (result.IfFail(out var e, out _))
                        logger.Warning(e, "Cannot find {TunnelId} in the storage. Probably bug!", update.From.Id);
                    break;

                case EntryRemoved<TunnelConfig> delete:
                    vm.TunnelConfigs.Remove(item => item.Config.Id == delete.OldValue.Id);
                    break;

                default:
                    throw new NotSupportedException($"Change type {change.GetType()} is not supported.");
            }
        };

    ConnectionInfoPanelViewModel CreateInfoVm(TunnelConfig config, bool initialPlaying = default) {
        var vm = new ConnectionInfoPanelViewModel(config);
        vm.Edit.Subscribe(c => EditConnection(c));
        vm.PlayOrStop.SubscribeAsync(isPlaying => (isPlaying ? Stop(vm) : Play(vm))
                                                | ifFail(LogError("play or stop")));
        var isPlaying = sshManager.TunnelRunningStateChanges
                                  .Where(state => state.Config.Id == vm.Config.Id)
                                  .Select(state => state.IsRunning)
                                  .StartWith(initialPlaying);
        vm.SetIsPlaying(isPlaying);
        return vm;
    }

    Unit EditConnection(Option<TunnelConfig> config = default) {
        var view = new TunnelConfigViewModel(config.IfNone(TunnelConfig.CreateSample));
        view.Save.SubscribeAsync(c => Update(c) | ifFail(LogError("saving tunnel config")));
        view.Back.Subscribe(_ => mainWindow.CloseCurrentView());
        view.Delete.SubscribeAsync(_ => Delete(view.Config.Id!.Value) | ifFail(LogError("deleting tunnel")));
        mainWindow.PushView(view);
        return unit;
    }

    Aff<Unit> Update(TunnelConfig config) {
        var updated = config.Id is null
                          ? sshManager.AddTunnel(config with { Id = uniqueId.NewGuid() })
                          : sshManager.UpdateTunnel(config);
        return from _1 in updated
               from _2 in Eff(mainWindow.CloseCurrentView)
               select unit;
    }

    Aff<Unit> Delete(Guid id) =>
        from _1 in sshManager.DeleteTunnel(id)
        from _2 in Eff(mainWindow.CloseCurrentView)
        select unit;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Aff<Unit> Play(ConnectionInfoPanelViewModel vm) =>
        sshManager.StartTunnel(vm.Config.Id!.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Aff<Unit> Stop(ConnectionInfoPanelViewModel vm) =>
        sshManager.StopTunnel(vm.Config.Id!.Value);

    Func<Error, Unit> LogError(string action) =>
        e => {
            logger.Error(e, "Error while {Action}", action);
            return unit;
        };
}