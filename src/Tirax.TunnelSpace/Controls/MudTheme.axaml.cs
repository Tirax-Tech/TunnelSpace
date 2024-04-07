using System;
using Avalonia.Markup.Xaml;
using Material.Styles.Themes;

namespace Tirax.TunnelSpace.Controls;

public class MudTheme : MaterialTheme
{
    public MudTheme(IServiceProvider serviceProvider) : base(serviceProvider) {
        AvaloniaXamlLoader.Load(this);
    }
}