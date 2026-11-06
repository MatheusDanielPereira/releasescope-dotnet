using System.Security.Cryptography;
using System.Text;

namespace ReleaseScope.Api.Webhooks;

public sealed class GitHubSignatureVerifier
{
    private const string Prefix = "sha256=";

    public bool IsValid(ReadOnlySpan<byte> payload, string? signatureHeader, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret) ||
            string.IsNullOrWhiteSpace(signatureHeader) ||
            !signatureHeader.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var signatureHex = signatureHeader[Prefix.Length..];
        if (signatureHex.Length != 64)
        {
            return false;
        }

        byte[] suppliedSignature;
        try
        {
            suppliedSignature = Convert.FromHexString(signatureHex);
        }
        catch (FormatException)
        {
            return false;
        }

        if (suppliedSignature.Length != 32)
        {
            return false;
        }

        var key = Encoding.UTF8.GetBytes(secret);
        Span<byte> expectedSignature = stackalloc byte[32];
        HMACSHA256.HashData(key, payload, expectedSignature);

        return CryptographicOperations.FixedTimeEquals(expectedSignature, suppliedSignature);
    }
}
