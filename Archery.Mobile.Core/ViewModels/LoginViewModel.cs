using Archery.Mobile.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

public sealed partial class LoginViewModel(
    IAuthService auth,
    INavigationService navigation,
    ILogger<LoginViewModel> logger) : BaseViewModel(logger)
{
    /// <summary>True while the stored session is being checked, so the page can hide its button.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotRestoring))]
    public partial bool IsRestoring { get; set; }

    public bool IsNotRestoring => !IsRestoring;

    public override async Task OnAppearingAsync(CancellationToken ct = default)
    {
        Title = "Archery Club";
        IsRestoring = true;
        try
        {
            if (await auth.TryRestoreSessionAsync(ct))
                await navigation.GoToRootAsync(Routes.Dashboard);
        }
        catch (Exception ex)
        {
            // A broken store must never block the sign-in button; the user can just sign in again.
            Logger.LogWarning(ex, "Could not restore the stored session");
        }
        finally
        {
            IsRestoring = false;
        }
    }

    [RelayCommand]
    async Task SignInAsync(CancellationToken ct)
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            if (await auth.SignInAsync(ct))
                await navigation.GoToRootAsync(Routes.Dashboard);
            else
                ErrorMessage = "Sign-in was cancelled.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Sign-in failed");
#if DEBUG
            // Diagnosing an OIDC failure from a generic message means guessing; on a device the
            // logger output is not readily visible either. Debug builds show the real reason.
            ErrorMessage = $"Could not sign in: {ex.Message}";
#else
            ErrorMessage = "Could not sign in. Check that the server is reachable and try again.";
#endif
        }
        finally
        {
            IsBusy = false;
        }
    }
}
