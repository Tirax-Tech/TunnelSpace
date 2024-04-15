using System.Collections.Generic;
using System.Reactive.Linq;
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

    readonly ObservableAsPropertyHelper<bool> showMenu;

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
            Text = text
        };

    public MainWindowViewModel() {
        CloseCurrentView = from v in this.ChangeProperties(ViewChangeProperties, Eff(history.Pop))
                           from _2 in UiEff(() => Header = GetViewHeader(history.Peek()))
                           select v;
        history.Push(new LoadingScreenViewModel());
        header = CreateTitleText(AppHeader);

        showMenu = this.WhenAnyValue(x => x.CurrentViewModel)
                       .Select(_ => history.Count == 1)
                       .ToProperty(this, x => x.ShowMenu);

        BackCommand = ReactiveCommand.CreateFromTask<Unit, Unit>(async _ => await CloseCurrentView.RunUnit());
    }

    public string Title {
        get => title;
        set => this.RaiseAndSetIfChanged(ref title, value);
    }

    public object Header {
        get => header;
        set => this.RaiseAndSetIfChanged(ref header, value);
    }

    public PageModelBase CurrentViewModel => history.Peek();

    public ReactiveCommand<Unit,Unit> BackCommand { get; }

    public bool ShowMenu => showMenu.Value;

    public Aff<PageModelBase> PushView(PageModelBase view) =>
        from v in this.ChangeProperty(nameof(CurrentViewModel), PushViewEff(view))
        from _1 in UiEff(() => Header = GetViewHeader(view))
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