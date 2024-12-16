using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;
using Tirax.TunnelSpace.Helpers;
using Dispatcher = Avalonia.Threading.Dispatcher;

namespace Tirax.TunnelSpace.ViewModels;

public readonly record struct SidebarItem(string Name, Func<Task<PageModelBase>> GetPage)
{
    public static implicit operator SidebarItem((string Name, Func<Task<PageModelBase>> GetPage) tuple) =>
        new(tuple.Name, tuple.GetPage);
}

public interface IAppMainWindow
{
    ValueTask<Unit> CloseCurrentView();
    ValueTask<Unit> PushView(PageModelBase replacement);
    ValueTask<Unit> Reset(PageModelBase replacement);

    void SetSidebar(IEnumerable<SidebarItem> items);
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

        BackCommand = ReactiveCommand.CreateFromTask<Unit, Unit>(async _ => await CloseCurrentView());
        GotoPageCommand = ReactiveCommand.CreateFromTask<string, Outcome<Unit>>(async page => {
            if (SidebarItems.Find(x => x.Name == page).IfSome(out var sidebar)){
                var view = await sidebar.GetPage();
                return await TryCatch(async () => await Reset(view));
            }
            return unit;
        });
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

    public ValueTask<Unit> PushView(PageModelBase view) =>
        RunOnUiThread(() => ChangeView(() => {
                       history.Push(view);
                       return unit;
                   }));

    static object GetViewHeader(PageModelBase view) =>
        view.Header switch
        {
            null     => CreateTitleText(AppHeader),
            string s => CreateTitleText(s),
            _        => view.Header
        };

    public ValueTask<Unit> CloseCurrentView() =>
        RunOnUiThread(() => ChangeView(ToUnit(history.Pop)));

    public ValueTask<Unit> Reset(PageModelBase replacement) {
        return RunOnUiThread(run);

        Unit run() => ChangeView(() => {
                              history.Clear();
                              history.Push(replacement);
                              return unit;
                          });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Unit ChangeView(Func<Unit> change) {
        this.ChangeProperty(nameof(CurrentViewModel), change);
        return RefreshHeader();
    }

    static async ValueTask<Unit> RunOnUiThread(Func<Unit> action) =>
        Dispatcher.UIThread.CheckAccess()
            ? action()
            : await Dispatcher.UIThread.InvokeAsync(action);

    #endregion

    #region Menu sidebar

    SidebarItem[] sidebarItems = [];

    public SidebarItem[] SidebarItems => sidebarItems;

    public void SetSidebar(IEnumerable<SidebarItem> items)
        => this.RaiseAndSetIfChanged(ref sidebarItems, items.ToArray(), nameof(SidebarItems)).Ignore();

    public ReactiveCommand<string, Outcome<Unit>> GotoPageCommand { get; }

    #endregion
}