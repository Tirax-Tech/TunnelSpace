using System;
using System.ComponentModel;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.Flows;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class ConnectionInfoPanelViewModel : ViewModelBase
{
    TunnelConfig tunnelConfig;

    bool isPlaying;
    string name = "(Sample name)";

    [DesignOnly(true)]
    public ConnectionInfoPanelViewModel() : this(AppCommands.EditDummy, TunnelConfig.CreateSample(Guid.Empty)) { }

    public ConnectionInfoPanelViewModel(ReactiveCommand<TunnelConfig,TunnelConfig> editCommand, TunnelConfig tunnelConfig)
    {
        Edit = editCommand;
        this.tunnelConfig = tunnelConfig;
        Name = tunnelConfig.Name;

        PlayOrStop = ReactiveCommand.Create<Unit,bool>(_ => IsPlaying = !IsPlaying);
    }

    public string Name {
        get => name;
        set => this.RaiseAndSetIfChanged(ref name, value);
    }

    public TunnelConfig Model {
        get => tunnelConfig;
        set => this.RaiseAndSetIfChanged(ref tunnelConfig, value);
    }

    public bool IsPlaying
    {
        get => isPlaying;
        set => this.RaiseAndSetIfChanged(ref isPlaying, value);
    }

    public TunnelConfig Config => tunnelConfig;

    public ReactiveCommand<Unit,bool> PlayOrStop { get; }
    public ReactiveCommand<TunnelConfig, TunnelConfig> Edit { get; }
}