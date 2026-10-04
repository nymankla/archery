using Archery.Mobile.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

public sealed partial class ExternalParticipantEditViewModel(
    IArcheryApiClient api,
    INavigationService navigation,
    ILogger<ExternalParticipantEditViewModel> logger) : BaseViewModel(logger)
{
    Guid? _participantId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial string FirstName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial string LastName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Email { get; set; }

    [ObservableProperty]
    public partial string? Phone { get; set; }

    [ObservableProperty]
    public partial string? ClubAffiliation { get; set; }

    public bool IsNew => _participantId is null;

    public bool CanSave =>
        !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName);

    public async Task InitialiseAsync(Guid? participantId, CancellationToken ct = default)
    {
        _participantId = participantId;
        Title = participantId is null ? "Add guest" : "Edit guest";

        if (participantId is null)
            return;

        await RunAsync(async token =>
        {
            // There is no GET by id for external participants, so the one being edited is
            // picked out of the list.
            var participants = await api.GetExternalParticipantsAsync(token) ?? [];
            var participant = participants.FirstOrDefault(p => p.Id == participantId.Value)
                ?? throw new InvalidOperationException("Guest not found.");

            FirstName = participant.FirstName;
            LastName = participant.LastName;
            Email = participant.Email;
            Phone = participant.Phone;
            ClubAffiliation = participant.ClubAffiliation;
        }, ct);
    }

    [RelayCommand]
    async Task SaveAsync(CancellationToken ct)
    {
        if (!CanSave)
            return;

        var participant = new ExternalParticipant
        {
            Id = _participantId ?? Guid.Empty,
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            Email = Trimmed(Email),
            Phone = Trimmed(Phone),
            ClubAffiliation = Trimmed(ClubAffiliation)
        };

        var saved = await RunAsync(token => _participantId is { } id
            ? api.UpdateExternalParticipantAsync(id, participant, token).EnsureArcherySuccessAsync(token)
            : api.CreateExternalParticipantAsync(participant, token).EnsureArcherySuccessAsync(token), ct);

        if (saved)
            await navigation.GoBackAsync();
    }

    [RelayCommand]
    Task CancelAsync() => navigation.GoBackAsync();

    static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
