using System;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class TunnelConfigViewModel : ViewModelBase
{
    string name, sshHost, remoteHost;
    short localPort, sshPort, remotePort;

    public TunnelConfigViewModel() : this(NewConfig()) { }

    static TunnelConfig NewConfig(Guid? id = default) =>
        new(id ?? Guid.NewGuid(), "localhost", 22, 2222, "localhost", 22, "New Tunnel");

    public ReactiveCommand<Unit, TunnelConfig> Save { get; }

    public ReactiveCommand<RUnit,RUnit> Back { get; } = ReactiveCommand.Create(() => {});

    public TunnelConfigViewModel(TunnelConfig config) {
        name = config.Name;
        sshHost = config.Host;
        remoteHost = config.RemoteHost;
        localPort = config.LocalPort;
        sshPort = config.Port;
        remotePort = config.RemotePort;

        var asTunnelConfig = Eff(() => new TunnelConfig(Guid.NewGuid(), sshHost, sshPort, localPort, remoteHost, remotePort, name));
        Save = ReactiveCommand.Create<Unit, TunnelConfig>(_ => asTunnelConfig.Run().ThrowIfFail());
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