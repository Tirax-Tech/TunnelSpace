using ReactiveUI;

namespace Tirax.TunnelSpace.ViewModels;

public class LoadingScreenViewModel : ViewModelBase
{
    string text = "Loading...";

    public LoadingScreenViewModel() { }

    public LoadingScreenViewModel(string text) {
        this.text = text;
    }

    public string Text
    {
        get => text;
        set => this.RaiseAndSetIfChanged(ref text, value);
    }
}