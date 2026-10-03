namespace Archery.Mobile.Views;

/// <summary>
/// Reads navigation parameters without going through Shell's type conversion.
/// </summary>
/// <remarks>
/// Pages take parameters by implementing <see cref="IQueryAttributable"/> rather than with
/// [QueryProperty]. Shell applies a [QueryProperty] by calling Convert.ChangeType, and Guid
/// does not implement IConvertible — so passing a Guid to a Guid? property throws
/// "Object must implement IConvertible" and takes the whole app down. IQueryAttributable hands
/// over the dictionary untouched, which sidesteps the conversion entirely.
///
/// Values may arrive as a Guid when navigating with a parameter dictionary, or as a string if
/// the route was ever built by hand, so both are accepted.
/// </remarks>
internal static class QueryAttributes
{
    public static Guid? GetGuid(this IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            Guid guid => guid,
            string text when Guid.TryParse(text, out var parsed) => parsed,
            _ => null
        };
    }
}
