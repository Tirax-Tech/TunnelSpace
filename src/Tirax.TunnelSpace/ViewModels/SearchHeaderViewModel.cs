using ReactiveUI;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class SearchHeaderViewModel : ViewModelBase
{
    string? text;

    public string? Text
    {
        get => text;
        set => this.RaiseAndSetIfChanged(ref text, value);
    }
}