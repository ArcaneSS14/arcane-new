using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Arcane.Server.Paint;

/// <summary>Downloads public images without giving a console command access to the server's private network.</summary>
public static class PaintImageDownloader
{
    private const int MaxDownloadBytes = 8 * 1024 * 1024;
    private const int MaxRedirects = 4;
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(15);

    // Special-purpose and transition ranges are not image sources, even where an anycast exception exists.
    private static readonly IPNetwork[] UnavailableNetworks =
    [
        IPNetwork.Parse("0.0.0.0/8"),
        IPNetwork.Parse("10.0.0.0/8"),
        IPNetwork.Parse("100.64.0.0/10"),
        IPNetwork.Parse("127.0.0.0/8"),
        IPNetwork.Parse("169.254.0.0/16"),
        IPNetwork.Parse("172.16.0.0/12"),
        IPNetwork.Parse("192.0.0.0/24"),
        IPNetwork.Parse("192.0.2.0/24"),
        IPNetwork.Parse("192.88.99.0/24"),
        IPNetwork.Parse("192.168.0.0/16"),
        IPNetwork.Parse("198.18.0.0/15"),
        IPNetwork.Parse("198.51.100.0/24"),
        IPNetwork.Parse("203.0.113.0/24"),
        IPNetwork.Parse("224.0.0.0/4"),
        IPNetwork.Parse("240.0.0.0/4"),
        IPNetwork.Parse("2001::/23"),
        IPNetwork.Parse("2001:db8::/32"),
        IPNetwork.Parse("2002::/16"),
        IPNetwork.Parse("3fff::/20"),
    ];
    private static readonly IPNetwork GlobalIpv6 = IPNetwork.Parse("2000::/3");
    private static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        UseCookies = false,
        ConnectCallback = ConnectPublic,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    });

    public static bool IsAllowedUri(Uri uri) =>
        uri.IsAbsoluteUri &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        string.IsNullOrEmpty(uri.UserInfo) &&
        (!IPAddress.TryParse(uri.DnsSafeHost, out var address) || IsPublicAddress(address));

    public static async Task<byte[]> Download(Uri uri)
    {
        using var cancel = new CancellationTokenSource(DownloadTimeout);
        for (var redirects = 0; ; redirects++)
        {
            if (!IsAllowedUri(uri))
                throw new InvalidDataException("The image URL is not public HTTP or HTTPS.");

            using var response = await Client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancel.Token);
            if ((int) response.StatusCode is >= 300 and < 400)
            {
                if (redirects >= MaxRedirects || response.Headers.Location is not { } location)
                    throw new InvalidDataException("Invalid image redirect.");
                var next = location.IsAbsoluteUri ? location : new Uri(uri, location);
                if (uri.Scheme == Uri.UriSchemeHttps && next.Scheme != Uri.UriSchemeHttps)
                    throw new InvalidDataException("Insecure image redirect.");
                uri = next;
                continue;
            }

            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > MaxDownloadBytes)
                throw new InvalidDataException("The image download is too large.");

            await using var stream = await response.Content.ReadAsStreamAsync(cancel.Token);
            using var memory = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(buffer, cancel.Token)) > 0)
            {
                if (memory.Length + read > MaxDownloadBytes)
                    throw new InvalidDataException("The image download is too large.");
                memory.Write(buffer, 0, read);
            }

            return memory.ToArray();
        }
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        if (address.AddressFamily == AddressFamily.InterNetworkV6 && !GlobalIpv6.Contains(address))
            return false;
        foreach (var network in UnavailableNetworks)
        {
            if (network.BaseAddress.AddressFamily == address.AddressFamily && network.Contains(address))
                return false;
        }
        return address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6;
    }

    private static async ValueTask<Stream> ConnectPublic(SocketsHttpConnectionContext context, CancellationToken cancel)
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, AddressFamily.Unspecified, cancel);
        foreach (var address in addresses)
        {
            if (!IsPublicAddress(address))
                throw new HttpRequestException("The image host resolves to a non-public address.");
        }

        SocketException? lastError = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                // Connect to the validated address itself; a second DNS lookup would permit rebinding.
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancel);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException error)
            {
                lastError = error;
                socket.Dispose();
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
        throw new HttpRequestException("No public image host connection succeeded.", lastError);
    }
}
