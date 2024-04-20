using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;
using Tirax.TunnelSpace.Helpers;

namespace Tirax.TunnelSpace.ViewModels;

public readonly record struct SidebarItem(string Name, Func<OutcomeAsync<PageModelBase>> GetPage)
{
    public static implicit operator SidebarItem((string Name, Func<OutcomeAsync<PageModelBase>> GetPage) tuple) =>
        new(tuple.Name, tuple.GetPage);
}

public interface IAppMainWindow
{
    Unit      CloseCurrentView();
    Unit      PushView(PageModelBase replacement);
    Unit      Reset(PageModelBase replacement);

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

    public MainWindowViewModel() {
        history.Push(new LoadingScreenViewModel());
        header = CreateTitleText(AppHeader);

        showMenu = this.WhenAnyValue(x => x.CurrentViewModel)
                       .Select(_ => history.Count == 1)
                       .ToProperty(this, x => x.ShowMenu);

        BackCommand = ReactiveCommand.Create<Unit, Unit>(_ => CloseCurrentView());
        GotoPageCommand = ReactiveCommand.CreateFromTask<string, Outcome<Unit>>
            (async page => await SidebarItems.Find(x1 => x1.Name == page)
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

    Unit RefreshHeader() {
        Header = GetViewHeader(history.Peek());
        return unit;
    }

    public Unit PushView(PageModelBase view) =>
        ChangeView(() => {
                       history.Push(view);
                       return unit;
                   });

    static object GetViewHeader(PageModelBase view) =>
        view.Header switch
        {
            null     => CreateTitleText(AppHeader),
            string s => CreateTitleText(s),
            _        => view.Header
        };

    public Unit CloseCurrentView() =>
        ChangeView(ToUnit(history.Pop));

    public Unit Reset(PageModelBase replacement) =>
        ChangeView(() => {
                       history.Clear();
                       history.Push(replacement);
                       return unit;
                   });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Unit ChangeView(Func<Unit> change) {
        this.ChangeProperty(nameof(CurrentViewModel), change);
        return RefreshHeader();
    }

    #endregion

    #region Menu sidebar

    Seq<SidebarItem> sidebarItems;

    public Seq<SidebarItem> SidebarItems => sidebarItems;

    public Unit SetSidebar(Seq<SidebarItem> items) =>
        ___(this.RaiseAndSetIfChanged(ref sidebarItems, items, nameof(SidebarItems)));

    public ReactiveCommand<string, Outcome<Unit>> GotoPageCommand { get; }

    #endregion
}