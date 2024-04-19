using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace.ViewModels;

public readonly record struct SidebarItem(string Name, Func<EitherAsync<Error,PageModelBase>> GetPage)
{
    public static implicit operator SidebarItem((string Name, Func<EitherAsync<Error, PageModelBase>> GetPage) tuple) =>
        new(tuple.Name, tuple.GetPage);
}

public interface IAppMainWindow
{
    Aff<Unit> CloseCurrentView { get; }
    Aff<Unit> PushView(PageModelBase replacement);
    Aff<Unit> Replace(PageModelBase replacement);
    Unit      Reset(PageModelBase replacement);

    Eff<Unit> SetSidebarEff(Seq<SidebarItem> items);
    Unit SetSidebar(Seq<SidebarItem> items);
}

public sealed class MainWindowViewModel : ViewModelBase, IAppMainWindow
{
    #region View controller

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

    public MainWindowViewModel() {
        RefreshHeader = UiEff(() => Header = GetViewHeader(history.Peek())).Ignore();
        CloseCurrentView = ChangeViewEff(Eff(history.Pop).Ignore());

        history.Push(new LoadingScreenViewModel());
        header = CreateTitleText(AppHeader);

        showMenu = this.WhenAnyValue(x => x.CurrentViewModel)
                       .Select(_ => history.Count == 1)
                       .ToProperty(this, x => x.ShowMenu);

        BackCommand = ReactiveCommand.CreateFromTask<Unit, Unit>(async _ => await CloseCurrentView.RunUnit());
        GotoPageCommand = ReactiveCommand.CreateFromTask<string, Either<Error, Unit>>(async page => await SidebarItems.Find(x1 => x1.Name == page)
                                                                                                                      .Map(x2 => x2.GetPage().Map(Reset))
                                                                                                                      .IfNone(unit));
    }

    static TextBlock CreateTitleText(string text) =>
        new()
        {
            Classes = { "Headline5" },
            VerticalAlignment = VerticalAlignment.Center,
            Text = text
        };

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

    Aff<Unit> RefreshHeader { get; }

    Aff<Unit> ChangeViewEff(Eff<Unit> change) =>
        from _1 in this.ChangeProperties(ViewChangeProperties, change)
        from _2 in RefreshHeader
        select unit;

    Eff<Unit> PushViewEff(PageModelBase view) =>
        eff(() => history.Push(view));

    public Aff<Unit> PushView(PageModelBase view) =>
        ChangeViewEff(PushViewEff(view));

    static object GetViewHeader(PageModelBase view) =>
        view.Header switch
        {
            null     => CreateTitleText(AppHeader),
            string s => CreateTitleText(s),
            _        => view.Header
        };

    public Aff<Unit> CloseCurrentView { get; }

    public Aff<Unit> Replace(PageModelBase replacement) =>
        ChangeViewEff(from _1 in Eff(history.Pop)
                   from _2 in PushViewEff(replacement)
                   select unit);

    public Aff<Unit> ResetEff(PageModelBase replacement) =>
        ChangeViewEff(from _1 in eff(history.Clear)
                   from _2 in PushViewEff(replacement)
                   select unit);

    public Unit Reset(PageModelBase replacement) =>
        ChangeView(() => {
                       history.Clear();
                       history.Push(replacement);
                       return unit;
                   });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Unit ChangeView(Func<Unit> change) =>
        this.ChangeProperty(nameof(CurrentViewModel), change);

    #endregion

    #region Menu sidebar

    Seq<SidebarItem> sidebarItems;

    public Seq<SidebarItem> SidebarItems => sidebarItems;

    public Eff<Unit> SetSidebarEff(Seq<SidebarItem> items) =>
        eff(() => this.RaiseAndSetIfChanged(ref sidebarItems, items, nameof(SidebarItems)));

    public Unit SetSidebar(Seq<SidebarItem> items) =>
        Void(this.RaiseAndSetIfChanged(ref sidebarItems, items, nameof(SidebarItems)));

    public ReactiveCommand<string, Either<Error, Unit>> GotoPageCommand { get; }

    #endregion
}