using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Tirax.TunnelSpace.EffHelpers;

public static class CollectionEff
{
    public static Eff<V> GetEff<K, V>(this IDictionary<K, V> dict, K key, Option<string> keyName = default) =>
        from getValue in Eff(() => {
                                 var success = dict.TryGetValue(key, out var v);
                                     return success
                                            ? SuccessEff(v)
                                            : FailEff<V>(keyName.Map(AppStandardErrors.NotFoundFromKey).IfNone(AppStandardErrors.NotFound));
                             })
        from v in getValue
        select v;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<Unit> Set<K,V>(this IDictionary<K,V> dict, K key, V value) =>
        eff(() => dict[key] = value);

    public static Eff<Option<V>> Set<K,V>(this ConcurrentDictionary<K,V> dict, K key, V value)
        where K: notnull =>
        Eff(() => {
                Option<V> old = None;
                dict.AddOrUpdate(key, value, (_, ov) => {
                                                 old = ov;
                                                 return value;
                                             });
                return old;
            });

    public static Eff<V> TakeOut<K, V>(this IDictionary<K, V> dict, K key) =>
        from value in dict.GetEff(key)
        from _1 in Eff(() => dict.Remove(key))
        select value;
}