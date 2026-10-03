using System.Collections.ObjectModel;
using System.Globalization;
using Archery.Mobile.Core.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Archery.Mobile.Core.ViewModels;

/// <summary>
/// Records or edits one archer's score.
/// </summary>
/// <remarks>
/// Like registration, the result belongs to exactly one of a member or an external participant
/// (CK_CompetitionResult_SingleParticipant). Opened from a registration it is pre-filled with
/// that archer's bow class, age class and gender, so the scorer confirms rather than retypes
/// what was already entered at sign-on.
///
/// Numeric fields are bound as strings and parsed explicitly: the app runs under sv-SE, and an
/// Entry bound straight to an int silently discards anything the current culture cannot parse.
/// </remarks>
public sealed partial class ResultEditViewModel(
    IArcheryApiClient api,
    INavigationService navigation,
    ILogger<ResultEditViewModel> logger) : BaseViewModel(logger)
{
    Guid _competitionId;
    Guid? _resultId;
    Guid? _memberId;
    Guid? _externalId;

    readonly List<ParticipantOption> _allMembers = [];
    readonly List<ParticipantOption> _allExternals = [];

    public ObservableCollection<ParticipantOption> Options { get; } = [];

    public IReadOnlyList<BowClass> BowClasses { get; } = Enum.GetValues<BowClass>();

    public IReadOnlyList<AgeClass> AgeClasses { get; } = Enum.GetValues<AgeClass>();

    public IReadOnlyList<Gender> Genders { get; } = Enum.GetValues<Gender>();

    [ObservableProperty]
    public partial ParticipantKind Kind { get; set; } = ParticipantKind.Member;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    public partial ParticipantOption? Selected { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial BowClass BowClass { get; set; } = BowClass.Recurve;

    [ObservableProperty]
    public partial AgeClass AgeClass { get; set; } = AgeClass.Senior;

    [ObservableProperty]
    public partial Gender Gender { get; set; } = Gender.Unknown;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(ScoreError))]
    [NotifyPropertyChangedFor(nameof(HasScoreError))]
    public partial string TotalScore { get; set; } = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(ScoreError))]
    [NotifyPropertyChangedFor(nameof(HasScoreError))]
    public partial string XCount { get; set; } = "0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(ScoreError))]
    [NotifyPropertyChangedFor(nameof(HasScoreError))]
    public partial string Placement { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsDisqualified { get; set; }

    [ObservableProperty]
    public partial string? Notes { get; set; }

    /// <summary>False when editing an existing result: the archer cannot be swapped afterwards.</summary>
    [ObservableProperty]
    public partial bool CanChooseParticipant { get; set; } = true;

    public bool IsMember => Kind == ParticipantKind.Member;

    public string? ScoreError
    {
        get
        {
            if (!TryParseInt(TotalScore, out var score) || score < 0)
                return "Total score must be a whole number of 0 or more.";

            if (!TryParseInt(XCount, out var xCount) || xCount < 0)
                return "X count must be a whole number of 0 or more.";

            if (xCount > score)
                return "X count cannot be greater than the total score.";

            if (!string.IsNullOrWhiteSpace(Placement)
                && (!TryParseInt(Placement, out var placement) || placement < 1))
            {
                return "Placement must be 1 or higher, or left blank.";
            }

            return null;
        }
    }

    public bool HasScoreError => ScoreError is not null;

    public bool CanSave => Selected is not null && ScoreError is null;

    public async Task InitialiseAsync(
        Guid competitionId, Guid? resultId, Guid? fromParticipantId, CancellationToken ct = default)
    {
        _competitionId = competitionId;
        _resultId = resultId;
        Title = resultId is null ? "Add result" : "Edit result";

        await RunAsync(async token =>
        {
            var members = await api.GetMembersAsync(token) ?? [];
            var externals = await api.GetExternalParticipantsAsync(token) ?? [];

            _allMembers.Clear();
            _allMembers.AddRange(members
                .OrderBy(m => m.FirstName).ThenBy(m => m.LastName)
                .Select(m => new ParticipantOption(m.Id, m.FullName, m.Email)));

            _allExternals.Clear();
            _allExternals.AddRange(externals
                .OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
                .Select(e => new ParticipantOption(e.Id, e.FullName, e.ClubAffiliation)));

            if (resultId is { } id)
                await LoadExistingAsync(id, token);
            else if (fromParticipantId is { } participantId)
                await PrefillFromParticipantAsync(participantId, token);

            ApplyFilter();
        }, ct);
    }

    async Task LoadExistingAsync(Guid resultId, CancellationToken ct)
    {
        var results = await api.GetResultsByCompetitionAsync(_competitionId, ct) ?? [];
        var result = results.FirstOrDefault(r => r.Id == resultId)
            ?? throw new InvalidOperationException("Result not found.");

        Apply(result.MemberId, result.ExternalParticipantId,
            result.BowClass, result.AgeClass, result.Gender);

        TotalScore = result.TotalScore.ToString(CultureInfo.CurrentCulture);
        XCount = result.XCount.ToString(CultureInfo.CurrentCulture);
        Placement = result.Placement?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
        IsDisqualified = result.IsDisqualified;
        Notes = result.Notes;

        // Changing who a recorded result belongs to is a different operation entirely; it would
        // also risk colliding with that archer's own result.
        CanChooseParticipant = false;
    }

    async Task PrefillFromParticipantAsync(Guid participantId, CancellationToken ct)
    {
        var participants = await api.GetParticipantsByCompetitionAsync(_competitionId, ct) ?? [];
        var participant = participants.FirstOrDefault(p => p.Id == participantId);

        if (participant is null)
            return;

        Apply(participant.MemberId, participant.ExternalParticipantId,
            participant.BowClass, participant.AgeClass, participant.Gender);
    }

    void Apply(Guid? memberId, Guid? externalId, BowClass bow, AgeClass age, Gender gender)
    {
        _memberId = memberId;
        _externalId = externalId;
        Kind = memberId.HasValue ? ParticipantKind.Member : ParticipantKind.External;
        BowClass = bow;
        AgeClass = age;
        Gender = gender;

        var id = memberId ?? externalId;
        var source = memberId.HasValue ? _allMembers : _allExternals;
        Selected = source.FirstOrDefault(o => o.Id == id);
    }

    [RelayCommand]
    async Task SaveAsync(CancellationToken ct)
    {
        if (!CanSave || Selected is null)
            return;

        TryParseInt(TotalScore, out var score);
        TryParseInt(XCount, out var xCount);
        int? placement = TryParseInt(Placement, out var parsedPlacement) ? parsedPlacement : null;

        var result = new CompetitionResult
        {
            Id = _resultId ?? Guid.Empty,
            CompetitionId = _competitionId,
            // Exactly one, never both — the same rule the database enforces.
            MemberId = IsMember ? Selected.Id : null,
            ExternalParticipantId = IsMember ? null : Selected.Id,
            BowClass = BowClass,
            AgeClass = AgeClass,
            Gender = Gender,
            TotalScore = score,
            XCount = xCount,
            Placement = placement,
            IsDisqualified = IsDisqualified,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
        };

        var saved = await RunAsync(token => _resultId is { } id
            ? api.UpdateResultAsync(id, result, token).EnsureArcherySuccessAsync(token)
            : api.CreateResultAsync(result, token).EnsureArcherySuccessAsync(token), ct);

        if (saved)
            await navigation.GoBackAsync();
    }

    [RelayCommand]
    Task CancelAsync() => navigation.GoBackAsync();

    partial void OnKindChanged(ParticipantKind value)
    {
        if (!CanChooseParticipant)
            return;

        Selected = null;
        OnPropertyChanged(nameof(IsMember));
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    void ApplyFilter()
    {
        IEnumerable<ParticipantOption> source = IsMember ? _allMembers : _allExternals;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            source = source.Where(o => o.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        Options.Clear();
        foreach (var option in source)
            Options.Add(option);
    }

    /// <summary>
    /// Accepts the current culture and, failing that, the invariant one. Android keyboards vary
    /// in which separator they offer, and sv-SE makes that difference visible.
    /// </summary>
    static bool TryParseInt(string? value, out int result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out result)
            || int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}
