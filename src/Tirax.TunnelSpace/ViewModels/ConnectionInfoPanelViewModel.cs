using System;
using System.ComponentModel;
using System.Reactive.Linq;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.Flows;

namespace Tirax.TunnelSpace.ViewModels;

public class ConnectionInfoPanelViewModel : ViewModelBase
{
    string name = "(Sample name)";
    ObservableAsPropertyHelper<bool> isPlaying;

    [DesignOnly(true)]
    public ConnectionInfoPanelViewModel() : this(TunnelConfig.CreateSample()) { }

    public ConnectionInfoPanelViewModel(TunnelConfig tunnelConfig)
    {
        Config = tunnelConfig;
        Name = tunnelConfig.Name;

        isPlaying = Observable.Empty<bool>().ToProperty(this, x => x.IsPlaying);

        PlayOrStop = ReactiveCommand.Create<Unit,bool>(_ => IsPlaying);
    }

    public string Name {
        get => name;
        set => this.RaiseAndSetIfChanged(ref name, value);
    }

    public bool IsPlaying => isPlaying.Value;

    public TunnelConfig Config { get; }

    public ReactiveCommand<Unit,bool> PlayOrStop { get; }
    public ReactiveCommand<TunnelConfig, TunnelConfig> Edit { get; } = AppCommands.CreateEdit();

    public Unit SetIsPlaying(IObservable<bool> state) {
        this.RaisePropertyChanging(nameof(IsPlaying));
        isPlaying = state.ToProperty(this, x => x.IsPlaying);
        this.RaisePropertyChanged(nameof(IsPlaying));
        return unit;
    }

    public Unit Stop() =>
        SetIsPlaying(Observable.Empty<bool>());
}