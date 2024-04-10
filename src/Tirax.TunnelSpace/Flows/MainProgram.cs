using System;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Flows;

static class AppCommands
{
    public static ReactiveCommand<TunnelConfig, TunnelConfig> CreateEdit() => ReactiveCommand.Create<TunnelConfig, TunnelConfig>(identity);
    public static readonly ReactiveCommand<TunnelConfig, TunnelConfig> EditDummy = CreateEdit();
}

sealed class MainProgram
{
    readonly ITunnelConfigStorage storage;

    public MainProgram(ConnectionSelectionFlow flowConnectionSelection, ITunnelConfigStorage storage) {
        this.storage = storage;

        Create =
            from vm in SuccessEff(new MainWindowViewModel())
            let afterInit =
                from initModel in flowConnectionSelection.Create
                from _2 in initModel.NewConnectionCommand.SubscribeEff(_ => EditConnection(vm))
                from _3 in initModel.Edit.SubscribeEff(config => EditConnection(vm, config))
                from _4 in vm.PushView(initModel)
                select unit
            from _ in afterInit.ToBackground()
            select vm;
    }

    public Eff<MainWindowViewModel> Create { get; }

    Eff<Unit> EditConnection(MainWindowViewModel vm, TunnelConfig? config = default) =>
        from view in SuccessEff(new TunnelConfigViewModel(config ?? TunnelConfig.CreateSample(Guid.Empty)))
        from _1 in view.Save.SubscribeEff(c => Update(vm, config is null, c).ToBackground())
        from _2 in view.Back.SubscribeEff(_ => vm.CloseCurrentView.Ignore())
        from _3 in vm.PushView(view)
        select unit;

    Aff<Unit> Update(MainWindowViewModel vm, bool isNew, TunnelConfig config) =>
        from _1 in isNew ? storage.Add(config) : storage.Update(config)
        from _2 in vm.CloseCurrentView
        select unit;
}
