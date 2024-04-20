using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
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
    public static Outcome<T> ReplaceFirst<T>(this ObservableCollection<T> collection, Predicate<T> itemToReplace, T newData) =>
        collection.ApplyEffectFirstItem(itemToReplace, i => collection[i] = newData);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Outcome<T> Remove<T>(this ObservableCollection<T> collection, Predicate<T> itemToDelete) =>
        collection.ApplyEffectFirstItem(itemToDelete, collection.RemoveAt);

    static Outcome<T> ApplyEffectFirstItem<T>(this IReadOnlyList<T> collection, Predicate<T> itemFinder, Action<int> effect) =>
        collection.TryFindIndex(itemFinder).ToOutcome()
                  .Map(i => {
                           var current = collection[i];
                           effect(i);
                           return current;
                       });
}