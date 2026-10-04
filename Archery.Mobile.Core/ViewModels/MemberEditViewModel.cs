using Archery.Mobile.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

/// <summary>
/// Add or edit a member.
/// </summary>
/// <remarks>
/// Client-side validation here is purely a UX layer and deliberately thin. The API validates
/// almost nothing beyond the personnummer, so these rules only save an obviously doomed round
/// trip; anything the server rejects is surfaced from its own response rather than guessed at.
/// In particular the personnummer checksum is left to the server, because reimplementing
/// PersonnummerParser here would drift from it.
/// </remarks>
public sealed partial class MemberEditViewModel(
    IArcheryApiClient api,
    INavigationService navigation,
    ILogger<MemberEditViewModel> logger) : BaseViewModel(logger)
{
    Guid? _memberId;

    public IReadOnlyList<BowClass> BowClasses { get; } = Enum.GetValues<BowClass>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial string FirstName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial string LastName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Address { get; set; }

    [ObservableProperty]
    public partial string? Phone { get; set; }

    [ObservableProperty]
    public partial string? Email { get; set; }

    [ObservableProperty]
    public partial string? Personnummer { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateError))]
    [NotifyPropertyChangedFor(nameof(HasDateError))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial DateTime DateOfBirth { get; set; } = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateError))]
    [NotifyPropertyChangedFor(nameof(HasDateError))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial DateTime JoinDate { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial BowClass PreferredBowClass { get; set; } = BowClass.Recurve;

    [ObservableProperty]
    public partial bool IsActive { get; set; } = true;

    public bool IsNew => _memberId is null;

    public string? DateError =>
        DateOfBirth.Date > DateTime.Today ? "Date of birth cannot be in the future."
        : JoinDate.Date < DateOfBirth.Date ? "Join date cannot be before the date of birth."
        : null;

    public bool HasDateError => DateError is not null;

    public bool CanSave =>
        !string.IsNullOrWhiteSpace(FirstName)
        && !string.IsNullOrWhiteSpace(LastName)
        && DateError is null;

    /// <summary>Loads an existing member, or prepares a blank one when <paramref name="memberId"/> is null.</summary>
    public async Task InitialiseAsync(Guid? memberId, CancellationToken ct = default)
    {
        _memberId = memberId;
        Title = memberId is null ? "Add member" : "Edit member";

        if (memberId is null)
        {
            // Matches the web app's defaults for a new member.
            DateOfBirth = DateTime.Today;
            JoinDate = DateTime.Today;
            return;
        }

        await RunAsync(async token =>
        {
            var member = await api.GetMemberAsync(memberId.Value, token)
                ?? throw new InvalidOperationException("Member not found.");

            FirstName = member.FirstName;
            LastName = member.LastName;
            Address = member.Address;
            Phone = member.Phone;
            Email = member.Email;
            Personnummer = member.Personnummer;
            DateOfBirth = member.DateOfBirth.ToDateTime(TimeOnly.MinValue);
            JoinDate = member.JoinDate.ToDateTime(TimeOnly.MinValue);
            PreferredBowClass = member.PreferredBowClass;
            IsActive = member.IsActive;
        }, ct);
    }

    [RelayCommand]
    async Task SaveAsync(CancellationToken ct)
    {
        if (!CanSave)
            return;

        var member = new Member
        {
            Id = _memberId ?? Guid.Empty,
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            Address = Trimmed(Address),
            Phone = Trimmed(Phone),
            Email = Trimmed(Email),
            Personnummer = Trimmed(Personnummer),
            DateOfBirth = DateOnly.FromDateTime(DateOfBirth),
            JoinDate = DateOnly.FromDateTime(JoinDate),
            PreferredBowClass = PreferredBowClass,
            IsActive = IsActive
        };

        var saved = await RunAsync(token => _memberId is { } id
            ? api.UpdateMemberAsync(id, member, token).EnsureArcherySuccessAsync(token)
            : api.CreateMemberAsync(member, token).EnsureArcherySuccessAsync(token), ct);

        if (saved)
            await navigation.GoBackAsync();
    }

    [RelayCommand]
    Task CancelAsync() => navigation.GoBackAsync();

    /// <summary>Empty optional fields go to the server as null rather than "".</summary>
    static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
