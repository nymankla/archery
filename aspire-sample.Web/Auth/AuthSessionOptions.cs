namespace aspire_sample.Web.Auth;

public class AuthSessionOptions
{
    public const string SectionName = "AuthSession";

    public int RefreshMinutes { get; set; } = 5;
    public int IdleTimeoutMinutes { get; set; } = 20;
    public int CookieExpirationMinutes { get; set; } = 20;
}
