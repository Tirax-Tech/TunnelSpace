using System.Collections.Concurrent;

namespace Tirax.TunnelSpace.EffHelpers;

public static class CollectionEff
{
    public static Eff<Option<TV>> SetEff<TK,TV>(this ConcurrentDictionary<TK,TV> dict, TK key, TV value)
        where TK: notnull =>
        Eff(() => {
                Option<TV> old = None;
                dict.AddOrUpdate(key, value, (_, ov) => {
                                                 old = ov;
                                                 return value;
                                             });
                return old;
            });
}