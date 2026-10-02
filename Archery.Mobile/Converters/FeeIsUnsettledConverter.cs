using System.Globalization;

namespace Archery.Mobile.Converters;

/// <summary>
/// True when a fee still needs paying, so the "Mark paid" action is shown only where it does
/// something. The view model also refuses to act on an already-paid fee; this keeps the button
/// from appearing at all, because offering an action that silently does nothing is worse than
/// not offering it.
/// </summary>
public sealed class FeeIsUnsettledConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is FeeStatus status && status != FeeStatus.Paid;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
