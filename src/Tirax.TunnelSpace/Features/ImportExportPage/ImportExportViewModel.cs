using ReactiveUI;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace.Features.ImportExportPage;

public sealed class ImportExportViewModel : PageModelBase
{
    public ReactiveCommand<Unit, Unit> ImportCommand { get; } = ReactiveCommand.Create<Unit, Unit>(_ => unit);
    public ReactiveCommand<Unit, Unit> ExportCommand { get; } = ReactiveCommand.Create<Unit, Unit>(_ => unit);
}