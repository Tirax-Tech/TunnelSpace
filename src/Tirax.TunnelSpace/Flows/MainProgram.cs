using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Flows;

sealed class MainProgram
{
    readonly TunnelConfigFlow flowTunnelConfig;
    readonly ITunnelConfigStorage storage;

    public MainProgram(ConnectionSelectionFlow flowConnectionSelection, TunnelConfigFlow flowTunnelConfig, ITunnelConfigStorage storage) {
        this.flowTunnelConfig = flowTunnelConfig;
        this.storage = storage;

        Create =
            from vm in SuccessEff(new MainWindowViewModel())
            let afterInit =
                from allData in storage.All
                from initModel in flowConnectionSelection.Create
                from _2 in initModel.NewConnectionCommand.SubscribeEff(_ => AddNewConnection(vm))
                from _3 in vm.PushView(initModel)
                select unit
            from _ in afterInit.ToBackground()
            select vm;
    }

    public Eff<MainWindowViewModel> Create { get; }

    Eff<Unit> AddNewConnection(MainWindowViewModel vm) =>
        from view in flowTunnelConfig.Create
        from _1 in view.Save.SubscribeEff(config => (from _1 in storage.Add(config)
                                                     from _2 in vm.CloseCurrentView
                                                     select unit
                                                    ).ToBackground())
        from _2 in view.Back.SubscribeEff(_ => vm.CloseCurrentView.Ignore())
        from _3 in vm.PushView(view)
        select unit;
}
