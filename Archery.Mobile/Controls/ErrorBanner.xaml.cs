using System.Windows.Input;

namespace Archery.Mobile.Controls;

/// <summary>
/// The one way this app reports a failure: the message produced by BaseViewModel, with an
/// optional retry. Shared so that "cannot reach the server" looks and behaves the same on
/// every screen rather than being re-invented per page.
/// </summary>
public partial class ErrorBanner : ContentView
{
    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(ErrorBanner));

    public static readonly BindableProperty RetryCommandProperty =
        BindableProperty.Create(nameof(RetryCommand), typeof(ICommand), typeof(ErrorBanner),
            propertyChanged: (b, _, _) => ((ErrorBanner)b).OnPropertyChanged(nameof(HasRetry)));

    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ICommand? RetryCommand
    {
        get => (ICommand?)GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }

    public bool HasRetry => RetryCommand is not null;

    public ErrorBanner() => InitializeComponent();
}
