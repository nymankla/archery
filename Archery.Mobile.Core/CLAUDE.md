# Archery.Mobile.Core

ViewModels (CommunityToolkit.Mvvm) and platform abstractions (`IAuthService`, `INavigationService`,
`IDialogService`, ...). No MAUI reference; that's what keeps it testable.

- Navigation parameters: implement `IQueryAttributable`, **never `[QueryProperty]`**. Shell applies it via
  `Convert.ChangeType` and `Guid` isn't `IConvertible`, so the process crashes.
- An `[ObservableProperty]` change handler that calls `RunAsync` does nothing when the property is set inside
  another `RunAsync` (`IsBusy` is already set, no re-entry). Await the work directly there.
- Numeric input: bind as strings and parse against current **and** invariant culture; under `sv-SE` an
  int-bound `Entry` silently drops input.
