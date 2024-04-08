using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Flows;

sealed class MainProgram
{
    readonly ITunnelConfigStorage storage;

    public MainProgram(ITunnelConfigStorage storage) {
        this.storage = storage;

        Run =
            from vm in Eff(() => new MainWindowViewModel())
            let afterInit =
                from allData in storage.All
                from initModel in SuccessEff(new ConnectionSelectionViewModel(allData))
                from _2 in initModel.NewConnectionCommand.SubscribeEff(_ => AddNewConnection(vm))
                from _3 in vm.PushView(initModel)
                select unit
            from _ in afterInit.ToBackground()
            select vm;
    }

    public Eff<MainWindowViewModel> Run { get; }

    Eff<Unit> AddNewConnection(MainWindowViewModel vm) =>
        from view in SuccessEff(new TunnelConfigViewModel())
        from _1 in view.Save.SubscribeEff(config => (from _1 in storage.Add(config)
                                                     from _2 in vm.CloseCurrentView
                                                     select unit
                                                    ).ToBackground())
        from _2 in view.Back.SubscribeEff(_ => vm.CloseCurrentView.Ignore())
        from _3 in vm.PushView(view)
        select unit;
}
