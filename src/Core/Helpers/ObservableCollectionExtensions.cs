using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using DynamicData;
using DynamicData.Kernel;
using RZ.Foundation.Functional;

namespace Tirax.TunnelSpace.Helpers;

public static class ObservableCollectionExtensions
{
    public static Outcome<T> Replace<T>(this ObservableCollection<T> collection, T oldData, T newData) {
        var index = collection.IndexOf(oldData);
        if (index == -1) return StandardErrors.NotFound;

        var current = collection[index];
        collection[index] = newData;
        return current;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Outcome<T> Replace<T>(this SourceCache<T, Guid> collection, Guid key, T newData) where T : notnull {
        var optValue = collection.Lookup(key);
        if (optValue.HasValue) {
            collection.AddOrUpdate(newData);
            return optValue.Value;
        }
        else
            return StandardErrors.NotFound;
    }

    static Outcome<T> ApplyEffectFirstItem<T>(this IReadOnlyList<T> collection, Predicate<T> itemFinder, Action<int> effect) =>
        collection.TryFindIndex(itemFinder).ToOutcome()
                  .Map(i => {
                           var current = collection[i];
                           effect(i);
                           return current;
                       });
}