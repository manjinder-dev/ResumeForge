using System.Net;
using System.Net.Sockets;

namespace ResumeForge.Api.Services;

public sealed class EndpointSecurityValidator : IEndpointSecurityValidator
{
    public async Task<IReadOnlyList<IPAddress>> ResolvePublicHttpsEndpointAsync(
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (!endpoint.IsAbsoluteUri ||
            !string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Custom AI endpoints must use an absolute HTTPS URL.");
        }

        if (!string.IsNullOrEmpty(endpoint.UserInfo))
        {
            throw new ArgumentException("Custom AI endpoints must not include credentials in the URL.");
        }

        if (string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Localhost is not allowed as a custom AI endpoint.");
        }

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(endpoint.DnsSafeHost, cancellationToken);
        }
        catch (SocketException ex)
        {
            throw new ArgumentException("The custom AI endpoint hostname could not be resolved.", ex);
        }

        if (addresses.Length == 0 || addresses.Any(IsNonPublicAddress))
        {
            throw new ArgumentException("The custom AI endpoint must resolve only to public internet addresses.");
        }

        return addresses;
    }

    private static bool IsNonPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            var first = bytes[0];
            var second = bytes[1];

            return first == 0 ||
                   first == 10 ||
                   first == 127 ||
                   (first == 100 && second is >= 64 and <= 127) ||
                   (first == 169 && second == 254) ||
                   (first == 172 && second is >= 16 and <= 31) ||
                   (first == 192 && second == 168) ||
                   (first == 198 && second is 18 or 19) ||
                   first >= 224;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            var uniqueLocal = (bytes[0] & 0xFE) == 0xFC;

            return uniqueLocal ||
                   address.IsIPv6LinkLocal ||
                   address.IsIPv6Multicast;
        }

        return true;
    }
}
