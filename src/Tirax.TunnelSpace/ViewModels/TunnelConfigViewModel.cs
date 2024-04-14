using System;
using System.ComponentModel;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class TunnelConfigViewModel : ViewModelBase
{
    [DesignOnly(true)]
    public TunnelConfigViewModel() : this(default) { }

    static TunnelConfig NewConfig(Guid id) =>
        new("localhost", 22, 2222, "localhost", 22, "New Tunnel", id);

    public ReactiveCommand<Unit, TunnelConfig> Delete { get; }
    public ReactiveCommand<Unit, TunnelConfig> Save { get; }
    public ReactiveCommand<Unit, Unit> Back { get; } = ReactiveCommand.Create<Unit,Unit>(_ => unit);

    internal TunnelConfigViewModel(Option<TunnelConfig> initial = default) {
        Config = initial.IfNone(() => NewConfig(Guid.Empty));

        Save = ReactiveCommand.Create<Unit, TunnelConfig>(_ => Config);
        Delete = ReactiveCommand.Create<Unit, TunnelConfig>(_ => Config);
    }

    public TunnelConfig Config { get; private set; }

    public string Name {
        get => Config.Name;
        set {
            this.RaisePropertyChanging();
            Config = Config with { Name = value };
            this.RaisePropertyChanged();
        }
    }

    public short LocalPort {
        get => Config.LocalPort;
        set {
            this.RaisePropertyChanging();
            Config = Config with { LocalPort = value };
            this.RaisePropertyChanged();
        }
    }

    public string SshHost {
        get => Config.Host;
        set {
            this.RaisePropertyChanging();
            Config = Config with { Host = value };
            this.RaisePropertyChanged();
        }
    }

    public short SshPort {
        get => Config.Port;
        set {
            this.RaisePropertyChanging();
            Config = Config with { Port = value };
            this.RaisePropertyChanged();
        }
    }

    public string RemoteHost {
        get => Config.RemoteHost;
        set {
            this.RaisePropertyChanging();
            Config = Config with { RemoteHost = value };
            this.RaisePropertyChanged();
        }
    }

    public short RemotePort {
        get => Config.RemotePort;
        set {
            this.RaisePropertyChanging();
            Config = Config with { RemotePort = value };
            this.RaisePropertyChanged();
        }
    }

    public bool IsNew => Config.Id is null;
    public string Title => IsNew ? "New Connection" : "Edit Connection";
}