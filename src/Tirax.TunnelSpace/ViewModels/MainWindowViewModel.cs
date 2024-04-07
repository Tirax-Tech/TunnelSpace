using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReactiveUI;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Services;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    readonly ServiceProviderEff sp;
    readonly Stack<ViewModelBase> history = new();

    public MainWindowViewModel(ServiceProviderEff sp) {
        this.sp = sp;

        CloseCurrentView = Eff(() => history.Pop());

        AddNewConnection = Eff(() => {
            var vm = CurrentViewModel;
            var newView = new TunnelConfigViewModel();
            history.Push(newView);

            newView.Save.Subscribe(config => this.ChangeView(nameof(CurrentViewModel), CloseCurrentView).RunUnit());

            this.RaiseAndSetIfChanged(ref vm, newView, nameof(CurrentViewModel));
            return unit;
        });

        var eff =
            from _1 in PushView(new LoadingScreenViewModel())
            from storage in sp.GetRequiredService<ITunnelConfigStorage>()
            from allData in storage.All
            from initModel in SuccessEff(new ConnectionSelectionViewModel(allData))
            from _2 in PushView(initModel)
            from _3 in initModel.NewConnectionCommand.SubscribeEff(_ => AddNewConnection)
            select unit;
        Task.Run(async () => await eff.RunUnit());
    }

    public ViewModelBase CurrentViewModel {
        get => history.Peek();
        set => Replace(value).RunUnit();
    }

    Eff<Unit> AddNewConnection { get; }

    Eff<ViewModelBase> PushView(ViewModelBase view) =>
        Eff(() => {
            history.Push(view);
            return view;
        });

    readonly Eff<ViewModelBase> CloseCurrentView;

    Eff<ViewModelBase> Replace(ViewModelBase replacement) =>
        this.ChangeView(nameof(CurrentViewModel),
                        from current in CloseCurrentView
                        from _ in PushView(replacement)
                        select current);
}
