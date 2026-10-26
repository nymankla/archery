using Archery.Mobile.Core.Abstractions;

namespace Archery.Mobile.Services;

public sealed class MauiDialogService : IDialogService
{
    static Page Page =>
        (Application.Current?.Windows is { Count: > 0 } windows ? windows[0].Page : null)
        ?? throw new InvalidOperationException("No active page to show a dialog on.");

    public Task AlertAsync(string title, string message, string cancel = "OK") =>
        MainThread.InvokeOnMainThreadAsync(() => Page.DisplayAlertAsync(title, message, cancel));

    public Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No") =>
        MainThread.InvokeOnMainThreadAsync(() => Page.DisplayAlertAsync(title, message, accept, cancel));
}
