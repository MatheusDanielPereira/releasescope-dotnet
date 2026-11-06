using System.Security.Cryptography;
using System.Text;
using ReleaseScope.Api.Webhooks;

namespace ReleaseScope.Api.Tests;

public sealed class GitHubSignatureVerifierTests
{
    private readonly GitHubSignatureVerifier _verifier = new();

    [Fact]
    public void IsValid_CorrectSignature_ReturnsTrue()
    {
        var body = Encoding.UTF8.GetBytes("{\"deployment\":{\"id\":42}}");
        const string secret = "unit-test-secret";
        var signature = CreateSignature(body, secret);

        var result = _verifier.IsValid(body, signature, secret);

        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sha1=0123456789")]
    [InlineData("sha256=not-hex")]
    [InlineData("sha256=0000000000000000000000000000000000000000000000000000000000000000")]
    public void IsValid_InvalidSignature_ReturnsFalse(string? signature)
    {
        var body = Encoding.UTF8.GetBytes("{}");

        var result = _verifier.IsValid(body, signature, "unit-test-secret");

        Assert.False(result);
    }

    private static string CreateSignature(byte[] body, string secret)
    {
        var digest = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
        return $"sha256={Convert.ToHexString(digest).ToLowerInvariant()}";
    }
}
