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
    Aff<Unit> Start { get; }
}

public sealed class MainProgram : IMainProgram
{
    public MainProgram(IAppMainWindow vm, IConnectionSelectionFlow flowConnectionSelection) {

        Start = from model in flowConnectionSelection.Create
                let sidebar = Seq<SidebarItem>(("Home", flowConnectionSelection.Create),
                                               ("Import/Export", Eff(() => (PageModelBase)new ImportExportViewModel())))
                from ____1 in vm.SetSidebar(sidebar)
                from ____2 in vm.Reset(model)
                select unit;
    }

    public Aff<Unit> Start { get; }
}
