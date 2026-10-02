namespace Archery.Mobile.Core.Abstractions;

/// <summary>Network reachability, so screens can explain an offline failure rather than just failing.</summary>
public interface IConnectivityService
{
    bool IsConnected { get; }

    event EventHandler<bool>? ConnectivityChanged;
}
