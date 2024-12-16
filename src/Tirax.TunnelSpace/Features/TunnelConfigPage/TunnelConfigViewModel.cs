using System;
using System.ComponentModel;
using System.Reactive.Linq;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Features.TunnelConfigPage;

public sealed class TunnelConfigViewModel : PageModelBase
{
    TunnelConfig config;
    readonly ObservableAsPropertyHelper<bool> isNew;

    [DesignOnly(true)]
    public TunnelConfigViewModel() : this(default) { }

    static TunnelConfig NewConfig(Guid id) =>
        new("localhost", 22, 2222, "localhost", 22, "New Tunnel", id);

    public ReactiveCommand<Unit, TunnelConfig> Delete { get; }
    public ReactiveCommand<Unit, TunnelConfig> Save { get; }
    public ReactiveCommand<Unit, Unit> Back { get; } = ReactiveCommand.Create<Unit,Unit>(_ => unit);

    internal TunnelConfigViewModel(Option<TunnelConfig> initial = default) {
        config = initial.IfNone(() => NewConfig(Guid.Empty));

        Save = ReactiveCommand.Create<Unit, TunnelConfig>(_ => config);
        Delete = ReactiveCommand.Create<Unit, TunnelConfig>(_ => config);

        isNew = this.WhenAnyValue(x => x.Config.Id)
                    .Select(x => x == Guid.Empty)
                    .ToProperty(this, x => x.IsNew);
        this.WhenAnyValue(x => x.IsNew)
            .Select(@new => @new ? "New Connection" : "Edit Connection")
            .Subscribe(title => Header = title);
    }

    public TunnelConfig Config {
        get => config;
        private set => this.RaiseAndSetIfChanged(ref config, value);
    }

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

    public bool IsNew => isNew.Value;
}