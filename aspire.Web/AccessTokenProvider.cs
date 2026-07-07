namespace aspire.Web;

public class AccessTokenProvider
{
    public const string PersistenceKey = "access_token";
    public string? AccessToken { get; set; }
}
