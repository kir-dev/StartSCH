using System.Net;
using System.Net.Sockets;

namespace StartSch;

/// Some SCH services have an AAAA record but don't listen on the returned IPv6 address. HttpClient connects to the IPv6
/// address then times out after 1 minute.
/// The Happy Eyeballs protocol is supposed to handle this, but it's not implemented in .NET.
public static class HappyEyeballs
{
    public const string HttpClient = nameof(HappyEyeballs) + nameof(HttpClient);
    
    /// based on https://github.com/dotnet/runtime/issues/26177#issuecomment-3070997810
    public static async ValueTask<Stream> HandlerConnectCallback(SocketsHttpConnectionContext ctx, CancellationToken ct)
    {
        DnsEndPoint dnsEndPoint = ctx.DnsEndPoint;

        var socketV4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true, };
        var socketV6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
            { NoDelay = true, DualMode = false };

        Task taskV4 = socketV4.ConnectAsync(dnsEndPoint, ct).AsTask();
        Task taskV6 = socketV6.ConnectAsync(dnsEndPoint, ct).AsTask();

        var t = await Task.WhenAny(taskV4, taskV6);
        if (!t.IsCompletedSuccessfully)
        {
            t = (t == taskV4) ? taskV6 : taskV4;
            await t.WaitAsync(ct); // this was sync for some reason (`t.Wait(ct)`)
        }

        if (!t.IsCompletedSuccessfully)
        {
            // surface exception
            socketV4.Dispose();
            socketV6.Dispose();
            await t;
        }

        if (t == taskV4)
        {
            // observe exception if any
            _ = taskV6.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
            socketV6.Dispose();
            return new NetworkStream(socketV4, ownsSocket: true);
        }
        else
        {
            // observe exception if any
            _ = taskV4.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
            socketV4.Dispose();
            return new NetworkStream(socketV6, ownsSocket: true);
        }
    }
}
