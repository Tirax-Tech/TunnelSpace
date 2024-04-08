using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Tirax.TunnelSpace.Domain;

namespace Tirax.TunnelSpace.Services;

public interface ITunnelConfigStorage
{
    Aff<Seq<TunnelConfig>> All { get; }
    Aff<TunnelConfig> Add(TunnelConfig config);

    IObservable<Change<TunnelConfig>> Changes { get; }
}

public class TunnelConfigStorage : ITunnelConfigStorage
{
    readonly ConcurrentDictionary<Guid, TunnelConfig> inMemoryStorage = new();
    readonly Subject<Change<TunnelConfig>> changes = new();

    public TunnelConfigStorage()
    {
        All = Eff(inMemoryStorage.Values.ToSeq);
    }

    public IObservable<Change<TunnelConfig>> Changes => changes;

    public Aff<Seq<TunnelConfig>> All { get; }

    public Aff<TunnelConfig> Add(TunnelConfig config) =>
        Eff(() => {
            inMemoryStorage[config.Id] = config;
            changes.OnNext(Change<TunnelConfig>.Added(config));
            return config;
        });
}