using Archery.Mobile.Core.Abstractions;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

/// <summary>
/// Registers an archer for a competition.
/// </summary>
/// <remarks>
/// Already-registered archers are filtered out of the list, which removes the most common cause
/// of a rejected save (CK_CompetitionParticipant_SingleParticipant and the unique indexes).
/// </remarks>
public sealed partial class ParticipantRegisterViewModel(
    IArcheryApiClient api,
    INavigationService navigation,
    ILogger<ParticipantRegisterViewModel> logger) : ParticipantFormViewModel(logger)
{
    Guid _competitionId;

    public override bool CanSave => Selected is not null;

    public async Task InitialiseAsync(Guid competitionId, CancellationToken ct = default)
    {
        _competitionId = competitionId;
        Title = "Register participant";

        await RunAsync(async token =>
        {
            var members = await api.GetMembersAsync(token) ?? [];
            var externals = await api.GetExternalParticipantsAsync(token) ?? [];
            var registered = await api.GetParticipantsByCompetitionAsync(_competitionId, token) ?? [];

            var takenMembers = registered.Where(p => p.MemberId.HasValue).Select(p => p.MemberId!.Value).ToHashSet();
            var takenExternals = registered.Where(p => p.ExternalParticipantId.HasValue)
                .Select(p => p.ExternalParticipantId!.Value).ToHashSet();

            SetOptions(
                members.Where(m => m.IsActive && !takenMembers.Contains(m.Id)),
                externals.Where(e => !takenExternals.Contains(e.Id)));
        }, ct);
    }

    [RelayCommand]
    async Task SaveAsync(CancellationToken ct)
    {
        if (Selected is null)
            return;

        var participant = new CompetitionParticipant
        {
            CompetitionId = _competitionId,
            MemberId = SelectedMemberId,
            ExternalParticipantId = SelectedExternalParticipantId,
            BowClass = BowClass,
            AgeClass = AgeClass,
            Gender = Gender
        };

        var saved = await RunAsync(token => api.RegisterParticipantAsync(participant, token), ct);

        if (saved)
            await navigation.GoBackAsync();
    }

    [RelayCommand]
    Task CancelAsync() => navigation.GoBackAsync();
}
