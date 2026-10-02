namespace Archery.Mobile.Core;

/// <summary>
/// Shell route names. Declared here so view models can navigate without referencing MAUI, and
/// so the strings exist once rather than in both projects.
/// </summary>
public static class Routes
{
    // Absolute routes reset the navigation stack.
    public const string Login = "//login";
    public const string Members = "//members";
    public const string TrainingAttendance = "//training";
    public const string TrainingHistory = "//traininghistory";

    // Pushed onto the stack; registered in AppShell's constructor.
    public const string MemberDetail = "memberdetail";
    public const string MemberEdit = "memberedit";

    /// <summary>Query key for the member id passed to detail and edit.</summary>
    public const string MemberIdKey = "memberId";
}
