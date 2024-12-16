using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Tirax.TunnelSpace.Domain;
using Unit = LanguageExt.Unit;

namespace Tirax.TunnelSpace.ViewModels;

public sealed class ConnectionSelectionViewModel : PageModelBase
{
    string searchKeyword = string.Empty;
    readonly SourceCache<ConnectionInfoPanelViewModel, Guid> allConnections;

    IReadOnlyCollection<ConnectionInfoPanelViewModel> tunnelConfigs = Array.Empty<ConnectionInfoPanelViewModel>();

    [DesignOnly(true)]
    public ConnectionSelectionViewModel() : this(Seq<ConnectionInfoPanelViewModel>(new(TunnelConfig.CreateSample()),
                                                                                   new(TunnelConfig.CreateSample()),
                                                                                   new(TunnelConfig.CreateSample()))) {
    }

    public ConnectionSelectionViewModel(IEnumerable<ConnectionInfoPanelViewModel> init) {
        var searchVm = new SearchHeaderViewModel();
        var keyword = searchVm.WhenAnyValue(x => x.Text).Select(k => k?.Trim());
        allConnections = new SourceCache<ConnectionInfoPanelViewModel, Guid>(m => m.Key);
        init.Iter(allConnections.AddOrUpdate);

        allConnections.Connect()
                      .ToCollection()
                      .CombineLatest(keyword, (configs, k) => (configs, keyword: k))
                      .Select(x => from c in x.configs
                                   where string.IsNullOrWhiteSpace(x.keyword) || c.Name.Contains(x.keyword, StringComparison.OrdinalIgnoreCase)
                                   select c)
                      .Subscribe(configs => {
                                     this.RaisePropertyChanging(nameof(TunnelConfigs));
                                     tunnelConfigs = configs.ToArray();
                                     this.RaisePropertyChanged(nameof(TunnelConfigs));
                                 });
        Header = searchVm;
    }

    public string SearchKeyword {
        get => searchKeyword;
        set => this.RaiseAndSetIfChanged(ref searchKeyword, value);
    }

    public SourceCache<ConnectionInfoPanelViewModel, Guid> AllConnections => allConnections;

    public IReadOnlyCollection<ConnectionInfoPanelViewModel> TunnelConfigs => tunnelConfigs;

    public ReactiveCommand<Unit, Unit> NewConnectionCommand { get; } = ReactiveCommand.Create<Unit,Unit>(_ => unit);
}