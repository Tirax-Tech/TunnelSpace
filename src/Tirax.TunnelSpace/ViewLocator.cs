using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace;

public class ViewLocator : IDataTemplate
{
    readonly Dictionary<Type, Type> viewLookup = new();

    public Control? Build(object? data)
    {
        Debug.Assert(data is not null);

        return viewLookup.Get(data.GetType())
                         .OrElse(() => ResolveViewAndCache(data.GetType()))
                         .Map(viewType => CreateView(viewType, data))
                         .IfNone(() => new TextBlock { Text = "View not found for: " + data.GetType().FullName });
    }

    public bool Match(object? data) => data is ViewModelBase;

    static Control CreateView(Type viewType, object data) {
        var control = (Control)Activator.CreateInstance(viewType)!;
        control.DataContext = data;
        return control;
    }

    Option<Type> ResolveViewAndCache(Type modelType) =>
        ViewTypeByFeature(modelType)
           .OrElse(() => ViewTypeByFolderConvention(modelType))
           .Map(viewType => {
                viewLookup[modelType] = viewType;
                return viewType;
            });

    static Option<Type> ViewTypeByFeature(Type modelType) =>
        from t in Some(modelType)
        where t.FullName!.EndsWith("ViewModel", StringComparison.Ordinal)
        from viewType in GetType(t.FullName![..(t.FullName!.Length - 9)])
        select viewType;

    static Option<Type> ViewTypeByFolderConvention(Type modelType) =>
        GetType(modelType.FullName!.Replace("ViewModel", "View", StringComparison.Ordinal));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Option<Type> GetType(string typeName) => Optional(Type.GetType(typeName));
}
