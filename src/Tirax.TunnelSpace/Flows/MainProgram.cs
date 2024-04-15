using ReactiveUI;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Flows;

static class AppCommands
{
    public static ReactiveCommand<TunnelConfig, TunnelConfig> CreateEdit() =>
        ReactiveCommand.Create<TunnelConfig, TunnelConfig>(identity);
}

public interface IMainProgram
{
    Aff<PageModelBase> Start { get; }
}

public sealed class MainProgram : IMainProgram
{
    public MainProgram(IAppMainWindow vm, IConnectionSelectionFlow flowConnectionSelection) {
        Start = from initModel in flowConnectionSelection.Create
                from _________ in vm.Replace(initModel)
                select initModel;
    }

    public Aff<PageModelBase> Start { get; }
}
