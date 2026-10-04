namespace Archery.Mobile.Core.Abstractions;

/// <summary>Alerts and confirmations, marshalled to the UI thread by the implementation.</summary>
public interface IDialogService
{
    Task AlertAsync(string title, string message, string cancel = "OK");

    Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "No");
}
