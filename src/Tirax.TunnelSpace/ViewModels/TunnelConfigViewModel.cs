using System;
using System.ComponentModel;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class TunnelConfigViewModel : ViewModelBase
{
    string name, sshHost, remoteHost;
    short localPort, sshPort, remotePort;

    [DesignOnly(true)]
    public TunnelConfigViewModel() : this(_ => SuccessAff(NewConfig())) { }

    static TunnelConfig NewConfig() =>
        new(Guid.Empty, "localhost", 22, 2222, "localhost", 22, "New Tunnel");

    public ReactiveCommand<Unit, TunnelConfig> Save { get; }
    public ReactiveCommand<Unit, Unit> Back { get; } = ReactiveCommand.Create<Unit,Unit>(_ => unit);

    internal TunnelConfigViewModel(Func<TunnelConfigViewModel, Aff<TunnelConfig>> save, Option<TunnelConfig> initial = default) {
        var config = initial.IfNone(NewConfig);
        name = config.Name;
        sshHost = config.Host;
        remoteHost = config.RemoteHost;
        localPort = config.LocalPort;
        sshPort = config.Port;
        remotePort = config.RemotePort;

        Save = ReactiveCommand.CreateFromTask<Unit, TunnelConfig>(async _ => (await save(this).Run()).ThrowIfFail());
    }

    public string Name {
        get => name;
        set => this.RaiseAndSetIfChanged(ref name, value);
    }

    public short LocalPort {
        get => localPort;
        set => this.RaiseAndSetIfChanged(ref localPort, value);
    }

    public string SshHost {
        get => sshHost;
        set => this.RaiseAndSetIfChanged(ref sshHost, value);
    }

    public short SshPort {
        get => sshPort;
        set => this.RaiseAndSetIfChanged(ref sshPort, value);
    }

    public string RemoteHost {
        get => remoteHost;
        set => this.RaiseAndSetIfChanged(ref remoteHost, value);
    }

    public short RemotePort {
        get => remotePort;
        set => this.RaiseAndSetIfChanged(ref remotePort, value);
    }
}