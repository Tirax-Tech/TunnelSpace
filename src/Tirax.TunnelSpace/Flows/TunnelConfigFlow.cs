using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Flows;

public sealed class TunnelConfigFlow
{
    readonly IUniqueId uniqueId;

    public TunnelConfigFlow(IUniqueId uniqueId) {
        this.uniqueId = uniqueId;
        Create = Eff(() => new TunnelConfigViewModel(OnSave));
    }

    public Eff<TunnelConfigViewModel> Create { get; }

    Aff<TunnelConfig> OnSave(TunnelConfigViewModel vm) =>
        from id in uniqueId.NewGuid
        select new TunnelConfig(id, vm.SshHost, vm.SshPort, vm.LocalPort, vm.RemoteHost, vm.RemotePort, vm.Name);
}