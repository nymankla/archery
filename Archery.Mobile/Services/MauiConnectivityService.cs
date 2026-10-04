using Archery.Mobile.Core.Abstractions;

namespace Archery.Mobile.Services;

public sealed class MauiConnectivityService : IConnectivityService
{
    public MauiConnectivityService() =>
        Connectivity.Current.ConnectivityChanged += (_, e) =>
            ConnectivityChanged?.Invoke(this, e.NetworkAccess == NetworkAccess.Internet);

    public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler<bool>? ConnectivityChanged;
}
