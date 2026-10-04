namespace Archery.Mobile.Core;

/// <summary>
/// Whether a competition entry belongs to a club member or a visiting archer.
/// </summary>
/// <remarks>
/// The database enforces this as exactly-one-of via the CK_*_SingleParticipant check
/// constraints: a participant or result carries either a MemberId or an ExternalParticipantId,
/// never both and never neither. Modelling it as a single choice rather than two nullable
/// fields is what keeps the UI from being able to express an invalid combination at all.
/// </remarks>
public enum ParticipantKind
{
    Member,
    External
}
