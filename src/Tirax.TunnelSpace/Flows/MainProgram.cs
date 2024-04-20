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
    OutcomeAsync<Unit> Start();
}

public sealed class MainProgram(IAppMainWindow vm, IConnectionSelectionFlow flowConnectionSelection) : IMainProgram
{
    public OutcomeAsync<Unit> Start() {
        var sidebar = Seq<SidebarItem>(("Home", flowConnectionSelection.Create),
                                       ("Import/Export", () => new ImportExportViewModel()));
        vm.SetSidebar(sidebar);

        return from model in flowConnectionSelection.Create()
               let _ = vm.Reset(model)
               select unit;
    }
}
