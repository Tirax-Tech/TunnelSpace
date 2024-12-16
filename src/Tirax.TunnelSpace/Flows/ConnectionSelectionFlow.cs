using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DynamicData;
using Serilog;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.Features.TunnelConfigPage;
using Tirax.TunnelSpace.Helpers;
using Tirax.TunnelSpace.Services.Akka;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Flows;

public interface IConnectionSelectionFlow
{
    Task<PageModelBase> Create();
}

public sealed class ConnectionSelectionFlow(ILogger logger, IAppMainWindow mainWindow, ISshManager sshManager,
                                            IUniqueId uniqueId) : IConnectionSelectionFlow
{
    public async Task<PageModelBase> Create() {
        var allData = await sshManager.RetrieveState();

        var configVms = allData.Map(i => CreateInfoVm(i.Config, i.IsRunning));
        var vm = new ConnectionSelectionViewModel(configVms);
        sshManager.Changes.Subscribe(ListenStorageChange(vm));
        vm.NewConnectionCommand.Subscribe(_ => EditConnection());
        return vm;
    }

    Action<LanguageExt.Change<TunnelConfig>> ListenStorageChange(ConnectionSelectionViewModel vm) =>
        change => {
            switch (change) {
                case EntryAdded<TunnelConfig> add:
                    vm.AllConnections.AddOrUpdate(CreateInfoVm(add.Value));
                    break;

                case EntryMapped<TunnelConfig, TunnelConfig> update:
                    var result = vm.AllConnections.Replace(update.From.Id, CreateInfoVm(update.To));
                    if (result is null)
                        logger.Warning("Cannot find {TunnelId} in the storage. Probably bug!", update.From.Id);
                    break;

                case EntryRemoved<TunnelConfig> delete:
                    vm.AllConnections.Edit(updater => updater.RemoveKey(delete.OldValue.Id));
                    break;

                default:
                    throw new NotSupportedException($"Change type {change.GetType()} is not supported.");
            }
        };

    ConnectionInfoPanelViewModel CreateInfoVm(TunnelConfig config, bool initialPlaying = default) {
        var vm = new ConnectionInfoPanelViewModel(config);
        vm.Edit.Subscribe(c => EditConnection(c));
        vm.PlayOrStop.SubscribeAsync(isPlaying => On(isPlaying ? Stop(vm) : Play(vm)).BeforeThrow(e => LogError(e, "play or stop")));
        var isPlaying = sshManager.TunnelRunningStateChanges
                                  .Where(state => state.Config.Id == vm.Config.Id)
                                  .Select(state => state.IsRunning)
                                  .StartWith(initialPlaying);
        vm.SetIsPlaying(isPlaying);
        return vm;
    }

    void EditConnection(Option<TunnelConfig> config = default) {
        var view = new TunnelConfigViewModel(config.IfNone(TunnelConfig.CreateSample));
        view.Save.SubscribeAsync(c => On(Update(c)).BeforeThrow(e => LogError(e, "saving tunnel config")));
        view.Back.Subscribe(_ => mainWindow.CloseCurrentView());
        view.Delete.SubscribeAsync(_ => On(Delete(view.Config.Id)).BeforeThrow(e => LogError(e, "deleting tunnel")));
        mainWindow.PushView(view);
    }

    async Task Update(TunnelConfig config) {
        await (config.Id == Guid.Empty
                   ? sshManager.AddTunnel(config with { Id = uniqueId.NewGuid() })
                   : sshManager.UpdateTunnel(config));
        await mainWindow.CloseCurrentView();
    }

    async Task Delete(Guid id) {
        await sshManager.DeleteTunnel(id);
        await mainWindow.CloseCurrentView();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Task Play(ConnectionInfoPanelViewModel vm) =>
        sshManager.StartTunnel(vm.Config.Id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Task Stop(ConnectionInfoPanelViewModel vm) =>
        sshManager.StopTunnel(vm.Config.Id);

    void LogError(Exception e, string action)
        => logger.Error(e, "Error while {Action}", action);
}