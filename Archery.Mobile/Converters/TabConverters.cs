using System.Globalization;

namespace Archery.Mobile.Converters;

/// <summary>
/// Colours the segmented control on the competition detail screen: the selected half is
/// filled, the other is outlined.
/// </summary>
/// <remarks>
/// Driven by the view model's ShowParticipants / ShowResults booleans rather than by visual
/// state, so which tab is active stays a property that can be asserted in a test.
/// </remarks>
public sealed class TabBackgroundConverter : IValueConverter
{
    static readonly Color Selected = Color.FromArgb("#512BD4");
    static readonly Color Unselected = Color.FromArgb("#22512BD4");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Selected : Unselected;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class TabForegroundConverter : IValueConverter
{
    static readonly Color OnSelected = Colors.White;
    static readonly Color OnUnselected = Color.FromArgb("#512BD4");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? OnSelected : OnUnselected;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>The other half of a two-button segmented control.</summary>
public sealed class InverseTabBackgroundConverter : IValueConverter
{
    readonly TabBackgroundConverter _inner = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        _inner.Convert(value is bool b && !b, targetType, parameter, culture);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class InverseTabForegroundConverter : IValueConverter
{
    readonly TabForegroundConverter _inner = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        _inner.Convert(value is bool b && !b, targetType, parameter, culture);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
