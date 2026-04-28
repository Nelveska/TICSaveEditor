using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TICSaveEditor.GUI.Converters;

/// <summary>
/// Maps a bool to an opacity value. True → 1.0, false → 0.45 (legible-but-dim).
/// Used to visually de-emphasise non-openable rows in the directory list.
/// FluentTheme's IsEnabled-driven dimming wasn't propagating to children inside
/// our ListBox.ItemTemplate, so this is the explicit fallback.
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    public static readonly BoolToOpacityConverter Instance = new();

    public double TrueValue { get; set; } = 1.0;
    public double FalseValue { get; set; } = 0.45;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TrueValue : FalseValue;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
