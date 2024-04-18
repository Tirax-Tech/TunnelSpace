using ReactiveUI;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.Features.ImportExportPage;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Flows;

static class AppCommands
{
    public static ReactiveCommand<TunnelConfig, TunnelConfig> CreateEdit() =>
        ReactiveCommand.Create<TunnelConfig, TunnelConfig>(identity);
}

public interface IMainProgram
{
    EitherAsync<Error, Unit> Start();
}

public sealed class MainProgram : IMainProgram
{
    readonly IAppMainWindow vm;
    readonly IConnectionSelectionFlow flowConnectionSelection;

    public MainProgram(IAppMainWindow vm, IConnectionSelectionFlow flowConnectionSelection) {
        this.vm = vm;
        this.flowConnectionSelection = flowConnectionSelection;
    }

    public EitherAsync<Error, Unit> Start() {
        var sidebar = Seq<SidebarItem>(("Home", Aff(async () => (await flowConnectionSelection.Create()).ToAff(identity)).Bind(identity)),
                                       ("Import/Export", Eff(() => (PageModelBase)new ImportExportViewModel())));
        vm.SetSidebar(sidebar).RunUnit();

         return from model in flowConnectionSelection.Create()
                let ____1 = vm.Reset(model).RunUnit()
                select unit;
    }
}
