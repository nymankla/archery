using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

/// <summary>
/// Shared busy/error plumbing. Every API call goes through <see cref="RunAsync"/> so that the
/// app has exactly one place where a failure becomes a message a user can act on.
/// </summary>
public abstract partial class BaseViewModel(ILogger logger) : ObservableObject
{
    protected ILogger Logger { get; } = logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(HasNoError))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    public bool IsNotBusy => !IsBusy;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>For views that show content only when there is nothing to report.</summary>
    public bool HasNoError => !HasError;

    /// <summary>Called when the page appears; override to load data.</summary>
    public virtual Task OnAppearingAsync(CancellationToken ct = default) => Task.CompletedTask;

    /// <summary>
    /// Runs an API operation, mapping every failure onto <see cref="ErrorMessage"/> and
    /// guaranteeing <see cref="IsBusy"/> is reset. Returns false when the operation failed.
    /// </summary>
    protected async Task<bool> RunAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken ct = default,
        bool showBusy = true)
    {
        if (IsBusy)
            return false;

        ErrorMessage = null;
        if (showBusy)
            IsBusy = true;

        try
        {
            await operation(ct);
            return true;
        }
        // ArcheryApiException derives from HttpRequestException, so these two clauses MUST stay
        // above the HttpRequestException one below. Reordering them silently loses the server's
        // own error message.
        catch (ArcheryApiException ex) when (ex.IsUnauthorized)
        {
            ErrorMessage = "Your session has expired. Please sign in again.";
            return false;
        }
        catch (ArcheryApiException ex)
        {
            ErrorMessage = ex.Error.Summary;
            return false;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            // Not the caller's cancellation: HttpClient surfaces its own timeout this way.
            ErrorMessage = "The server took too long to respond.";
            return false;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "Network failure calling the API");
            ErrorMessage = "Cannot reach the server. Check your connection and try again.";
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected failure");
            ErrorMessage = "Something went wrong.";
            return false;
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }
}
