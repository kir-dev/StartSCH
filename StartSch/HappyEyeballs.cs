using System.Net;
using System.Net.Sockets;

namespace StartSch;

// Some SCH services have an AAAA record but don't listen on the returned IPv6 address. HttpClient connects to the IPv6
// address then times out after 1 minute.
// The Happy Eyeballs protocol is supposed to handle this, but it's not implemented in .NET.
//
// Sources:
// RFC 6555: Happy Eyeballs: Success with Dual-Stack Hosts - https://www.rfc-editor.org/rfc/rfc6555
// RFC 8305: Happy Eyeballs Version 2: Better Connectivity Using Concurrency - https://www.rfc-editor.org/rfc/rfc8305
// IPv6 is hard: Happy Eyeballs and .NET HttpClient - https://slugcat.systems/post/24-06-16-ipv6-is-hard-happy-eyeballs-dotnet-httpclient/
// Sample implementation under one of the issues in the dotnet/runtime repo - https://github.com/dotnet/runtime/issues/26177#issuecomment-3070997810
public static class HappyEyeballs
{
    private static readonly TimeSpan ResolutionDelay = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan ConnectionAttemptDelay = TimeSpan.FromMilliseconds(250);

    public static async ValueTask<Stream> SocketsHttpHandlerConnectCallback(
        SocketsHttpConnectionContext ctx,
        CancellationToken ct)
    {
        if (IPAddress.TryParse(ctx.DnsEndPoint.Host, out IPAddress? _))
            throw new NotImplementedException();

        DnsEndPoint dnsEndPoint = ctx.DnsEndPoint;

        var v6Task = Dns.GetHostAddressesAsync(dnsEndPoint.Host, AddressFamily.InterNetworkV6, ct);
        var v4Task = Dns.GetHostAddressesAsync(dnsEndPoint.Host, AddressFamily.InterNetwork, ct);

        var completed = await Task.WhenAny(v6Task, v4Task);
        var other = completed == v6Task ? v4Task : v6Task;

        if (completed == v4Task)
            await Task.WhenAny(v6Task, Task.Delay(ResolutionDelay, ct));

        IPAddress? latestAttempt = null;
        List<Task<Socket>>? ongoingAttempts = null;
        List<Socket>? sockets = null;
        Queue<IPAddress>? v6Addresses = null;
        Queue<IPAddress>? v4Addresses = null;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            IPAddress? next = GetNext();
            Task<Socket> candidate;
            if (next == null)
            {
                if (!other.IsCompleted)
                {
                    await ((Task)other).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                    continue;
                }

                if (ongoingAttempts is not { Count: > 0 })
                {
                    // These exceptions might have to be revised if new issues come up
                    
                    // No addresses found so no sockets opened
                    if (sockets == null)
                        throw new SocketException((int)SocketError.HostNotFound);

                    throw new SocketException((int)SocketError.HostUnreachable);
                }

                candidate = await Task.WhenAny(ongoingAttempts);
            }
            else
            {
                Socket socket = new(next.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                (sockets ??= []).Add(socket);
                Task connectAsync = socket.ConnectAsync(next, dnsEndPoint.Port, ct).AsTask();
                (ongoingAttempts ??= []).Add(HandleConnectionResult(socket, connectAsync));
                latestAttempt = next;

                Task connectionAttemptDelayTask = Task.Delay(ConnectionAttemptDelay, ct);
                var task = await Task.WhenAny([..ongoingAttempts, connectionAttemptDelayTask]);
                if (task == connectionAttemptDelayTask)
                    continue;
                candidate = (Task<Socket>)task;
            }

            if (candidate.IsCompletedSuccessfully)
            {
                Socket winnerSocket = candidate.Result;
                if (sockets != null)
                    foreach (Socket socket in sockets)
                        if (socket != winnerSocket)
                            socket.Dispose();
                return new NetworkStream(winnerSocket, true);
            }

            ongoingAttempts.Remove(candidate);
        }

        IPAddress? GetNext()
        {
            if (v6Addresses == null && v6Task is { IsCompletedSuccessfully: true, Result.Length: > 0 })
                v6Addresses = new(v6Task.Result);
            if (v4Addresses == null && v4Task is { IsCompletedSuccessfully: true, Result.Length: > 0 })
                v4Addresses = new(v4Task.Result);

            bool preferV6 = latestAttempt is not { AddressFamily: AddressFamily.InterNetworkV6 };
            bool haveV6 = v6Addresses is { Count: > 0 };
            bool haveV4 = v4Addresses is { Count: > 0 };

            if ((preferV6 || !haveV4) && haveV6)
                return v6Addresses!.Dequeue();
            if (haveV4)
                return v4Addresses!.Dequeue();
            return null;
        }

        async Task<Socket> HandleConnectionResult(
            Socket socket,
            Task connectTask
        )
        {
            try
            {
                await connectTask;
                return socket;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }
}
