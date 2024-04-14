using System;
using System.ComponentModel;
using System.Reactive.Linq;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.Flows;
using Tirax.TunnelSpace.Services.Akka;

namespace Tirax.TunnelSpace.ViewModels;

public class ConnectionInfoPanelViewModel : ViewModelBase, IDisposable
{
    string name = "(Sample name)";
    ObservableAsPropertyHelper<bool> isPlaying;

    [DesignOnly(true)]
    public ConnectionInfoPanelViewModel() : this(TunnelConfig.CreateSample(Guid.Empty)) { }

    public ConnectionInfoPanelViewModel(TunnelConfig tunnelConfig)
    {
        Config = tunnelConfig;
        Name = tunnelConfig.Name;

        isPlaying = Observable.Empty<bool>().ToProperty(this, x => x.IsPlaying);

        PlayOrStop = ReactiveCommand.Create<Unit,bool>(_ => IsPlaying);
    }

    public Option<ISshController> Controller { get; private set; }

    public string Name {
        get => name;
        set => this.RaiseAndSetIfChanged(ref name, value);
    }

    public bool IsPlaying => isPlaying.Value;

    public TunnelConfig Config { get; }

    public ReactiveCommand<Unit,bool> PlayOrStop { get; }
    public ReactiveCommand<TunnelConfig, TunnelConfig> Edit { get; } = AppCommands.CreateEdit();

    public Unit Play(ISshController controller, IObservable<bool> state) {
        this.RaisePropertyChanging(nameof(IsPlaying));
        Controller = Some(controller);
        isPlaying = state.ToProperty(this, x => x.IsPlaying);
        this.RaisePropertyChanged(nameof(IsPlaying));
        return unit;
    }

    public Unit Stop() {
        this.RaisePropertyChanging(nameof(IsPlaying));
        Controller = None;
        isPlaying = Observable.Empty<bool>().ToProperty(this, x => x.IsPlaying);
        this.RaisePropertyChanged(nameof(IsPlaying));
        return unit;
    }

    public void Dispose() {
        Controller.Iter(c => c.Dispose());
    }
}