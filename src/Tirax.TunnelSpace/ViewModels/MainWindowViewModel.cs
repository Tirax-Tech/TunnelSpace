using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace.ViewModels;

public interface IAppMainWindow
{
    Aff<PageModelBase> CloseCurrentView { get; }
    Aff<PageModelBase> PushView(PageModelBase replacement);
    Aff<PageModelBase> Replace(PageModelBase replacement);
}

public sealed class MainWindowViewModel : ViewModelBase, IAppMainWindow
{
    readonly Stack<PageModelBase> history = new();
    string title = AppTitle;
    object header;

    static readonly string AppVersion =
        Assembly.GetEntryAssembly()!
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                .InformationalVersion
                .Split('+')[0];

    const string AppHeader = "Tunnel Space";
    static readonly string AppTitle = $"Tirax Tunnel Space {AppVersion}";

    static readonly Seq<string> ViewChangeProperties = Seq(nameof(CurrentViewModel), nameof(Header));

    static object CreateTitleText(string text) =>
        new TextBlock
        {
            Classes = { "Headline5" },
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = text
        };

    public MainWindowViewModel() {
        CloseCurrentView = from v in this.ChangeProperties(ViewChangeProperties, Eff(history.Pop))
                           from _2 in UiEff(() => Header = GetViewHeader(history.Pop()))
                           select v;
        history.Push(new LoadingScreenViewModel());
        header = CreateTitleText(AppHeader);
    }
    public string Title {
        get => title;
        set => this.RaiseAndSetIfChanged(ref title, value);
    }

    public object Header {
        get => header;
        set => this.RaiseAndSetIfChanged(ref header, value);
    }

    public PageModelBase CurrentViewModel {
        get => history.Peek();
        set => Replace(value).RunIgnore();
    }

    public Aff<PageModelBase> PushView(PageModelBase view) =>
        from _1 in UiEff(() => Header = GetViewHeader(view))
        from v in this.ChangeProperty(nameof(CurrentViewModel), PushViewEff(view))
        select v;

    static object GetViewHeader(PageModelBase view) =>
        view.Header switch
        {
            null     => CreateTitleText(AppHeader),
            string s => CreateTitleText(s),
            _        => view.Header
        };

    Eff<PageModelBase> PushViewEff(PageModelBase view) =>
        Eff(() => {
                history.Push(view);
                return view;
            });

    public Aff<PageModelBase> CloseCurrentView { get; }

    public Aff<PageModelBase> Replace(PageModelBase replacement) =>
        from v in this.ChangeProperties(ViewChangeProperties, from current in Eff(history.Pop)
                                                              from _ in PushViewEff(replacement)
                                                              select current)
        from _1 in UiEff(() => Header = GetViewHeader(history.Peek()))
        select v;
}