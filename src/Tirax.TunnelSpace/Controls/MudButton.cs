using System;
using Avalonia.Controls;

namespace Tirax.TunnelSpace.Controls;

public class MudButton : Button
{
    protected override Type StyleKeyOverride { get; } = typeof(Button);
}