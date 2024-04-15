using ReactiveUI;

namespace Tirax.TunnelSpace.ViewModels;

public abstract class ViewModelBase : ReactiveObject;

public abstract class PageModelBase(object? header = null) : ViewModelBase
{
    /// <summary>
    ///  Header to be displayed on the app bar.
    /// </summary>
    public object? Header {
        get => header;
        set => this.RaiseAndSetIfChanged(ref header, value);
    }
}
