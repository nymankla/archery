using System.Globalization;

namespace Archery.Mobile.Converters;

/// <summary>
/// Inverts a bool for visibility bindings.
/// </summary>
/// <remarks>
/// View models expose paired properties where the inverse is used often (IsNotBusy, HasNoError),
/// because that keeps compiled bindings simple. This converter covers the cases where the source
/// is a shared wire model, such as Member.IsActive, which has no business carrying a UI-only
/// inverse property.
/// </remarks>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : false;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : false;
}
