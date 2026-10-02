namespace Archery.Mobile.Core.Abstractions;

/// <summary>Navigation, expressed as routes so view models stay free of MAUI types.</summary>
public interface INavigationService
{
    Task GoToAsync(string route, IDictionary<string, object>? parameters = null);

    Task GoBackAsync();

    /// <summary>Resets the navigation stack to an absolute route, e.g. after sign-in or sign-out.</summary>
    Task GoToRootAsync(string route);
}
