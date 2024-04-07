using System.Collections.Concurrent;
using Tirax.TunnelSpace.Domain;

namespace Tirax.TunnelSpace.Services;

public interface ITunnelConfigStorage
{
    Aff<Seq<TunnelConfig>> All { get; }
    Aff<TunnelConfig> Add(TunnelConfig config);
}

public class TunnelConfigStorage : ITunnelConfigStorage
{
    readonly ConcurrentDictionary<Guid, TunnelConfig> inMemoryStorage = new();

    public TunnelConfigStorage()
    {
        All = Eff(inMemoryStorage.Values.ToSeq);
    }

    public Aff<Seq<TunnelConfig>> All { get; }

    public Aff<TunnelConfig> Add(TunnelConfig config) =>
        Eff(() => {
            inMemoryStorage[config.Id] = config;
            return config;
        });
}