using System;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class TunnelConfigViewModel(TunnelConfig config) : ViewModelBase
{
    string name = config.Name,
           sshHost = config.Host,
           remoteHost = config.RemoteHost;
    short localPort = config.LocalPort,
          sshPort = config.Port,
          remotePort = config.RemotePort;

    public TunnelConfigViewModel() : this(NewConfig()) { }

    static TunnelConfig NewConfig(Guid? id = default) =>
        new(id ?? Guid.NewGuid(), "localhost", 22, 2222, "localhost", 22, "New Tunnel");

    public ReactiveCommand<TunnelConfig, RUnit> Save { get; } = ReactiveCommand.Create<TunnelConfig, RUnit>(config => {
        // Save the config
        return RUnit.Default;
    });

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