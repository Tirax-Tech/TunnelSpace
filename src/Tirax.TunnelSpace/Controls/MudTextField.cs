using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Material.Styles.Assists;

namespace Tirax.TunnelSpace.Controls;

public sealed class MudTextField : TextBox
{
    protected override void OnLoaded(RoutedEventArgs e) {
        UseFloatingWatermark = true;
        Classes.AddRange(Seq("dense", "filled"));
        base.OnLoaded(e);
    }

    protected override void OnInitialized() {
        base.OnInitialized();
        Theme = this.TryFindResource("FilledTextBox", out var theme) ? (ControlTheme?) theme : null;
    }

    protected override Type StyleKeyOverride { get; } = typeof(TextBox);

    public static readonly DirectProperty<MudTextField, string?> HelperTextProperty =
        AvaloniaProperty.RegisterDirect<MudTextField, string?>(
            nameof(HelperText),
            o => o.HelperText,
            (o, v) => o.HelperText = v);

    public string? HelperText {
        get => (string?) GetValue(TextFieldAssist.HintsProperty);
        set => SetValue(TextFieldAssist.HintsProperty, value);
    }

    public string? Label {
        get => (string?) GetValue(TextFieldAssist.LabelProperty);
        set => SetValue(TextFieldAssist.LabelProperty, value);
    }
}