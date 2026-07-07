using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace aspire.Web.Auth;

public sealed class MemoryCacheTicketStore(IMemoryCache cache) : ITicketStore
{
    private static readonly TimeSpan FallbackLifetime = TimeSpan.FromHours(8);
    private const string KeyPrefix = "auth-ticket:";

    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = CreateKey();
        SetTicket(key, ticket);
        return Task.FromResult(key);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        SetTicket(key, ticket);
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        cache.TryGetValue(key, out AuthenticationTicket? ticket);
        return Task.FromResult(ticket);
    }

    public Task RemoveAsync(string key)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }

    private void SetTicket(string key, AuthenticationTicket ticket)
    {
        cache.Set(key, ticket, BuildCacheOptions(ticket));
    }

    private static string CreateKey() => $"{KeyPrefix}{Guid.NewGuid():N}";

    private static MemoryCacheEntryOptions BuildCacheOptions(AuthenticationTicket ticket)
    {
        var options = new MemoryCacheEntryOptions();
        var expiresUtc = ticket.Properties.ExpiresUtc;

        if (expiresUtc is { } expiration && expiration > DateTimeOffset.UtcNow)
        {
            options.SetAbsoluteExpiration(expiration);
        }
        else
        {
            options.SetAbsoluteExpiration(FallbackLifetime);
        }

        return options;
    }
}
