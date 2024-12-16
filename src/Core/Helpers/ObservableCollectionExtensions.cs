using System.Collections.ObjectModel;
using DynamicData;

namespace Tirax.TunnelSpace.Helpers;

public static class ObservableCollectionExtensions
{
    public static T? Replace<T>(this ObservableCollection<T> collection, T oldData, T newData) {
        var index = collection.IndexOf(oldData);
        if (index == -1) return default;

        var current = collection[index];
        collection[index] = newData;
        return current;
    }

    public static T? Replace<T>(this SourceCache<T, Guid> collection, Guid key, T newData) where T : notnull {
        var optValue = collection.Lookup(key);
        if (optValue.HasValue) {
            collection.AddOrUpdate(newData);
            return optValue.Value;
        }
        else
            return default;
    }

    static T? ApplyEffectFirstItem<T>(this IReadOnlyList<T> collection, Predicate<T> itemFinder, Action<int> effect)
        => collection.TryFindIndex(itemFinder)
                     .Map(i => {
                          var current = collection[i];
                          effect(i);
                          return current;
                      }).IfSome(out var v)
               ? v
               : default;
}