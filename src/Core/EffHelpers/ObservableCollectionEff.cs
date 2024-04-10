using System.Collections.ObjectModel;

namespace Tirax.TunnelSpace.EffHelpers;

public static class ObservableCollectionEff
{
    public static Eff<Unit> AddEff<T>(this ObservableCollection<T> collection, T data) =>
        Eff(() => {
            collection.Add(data);
            return unit;
        });

    public static Eff<T> ReplaceEff<T>(this ObservableCollection<T> collection, Predicate<T> itemToReplace, T newData) =>
        Eff(() =>
        {
            var index = collection.TryFindIndex(itemToReplace).Get();
            var current = collection[index];
            collection[index] = newData;
            return current;
        });

    public static Eff<T> ReplaceEff<T>(this ObservableCollection<T> collection, T oldData, T newData) =>
        Eff(() => {
            var index = collection.IndexOf(oldData);
            var current = collection[index];
            collection[index] = newData;
            return current;
        });

    public static Eff<bool> RemoveEff<T>(this ObservableCollection<T> collection, Predicate<T> itemToDelete) =>
        Eff(() =>
        {
            if (collection.TryFindIndex(itemToDelete).IfSome(out var index))
            {
                collection.RemoveAt(index);
                return true;
            }
            else
                return false;
        });

    public static Eff<bool> RemoveEff<T>(this ObservableCollection<T> collection, T data) =>
        Eff(() => collection.Remove(data));
}