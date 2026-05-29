namespace aspire_sample.Web;

public class AccessTokenProvider
{
    public const string PersistenceKey = "access_token";
    public string? AccessToken { get; set; }
}
