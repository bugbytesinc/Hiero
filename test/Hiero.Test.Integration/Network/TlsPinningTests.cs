using Hiero.Mirror;
using Hiero.Test.Integration.Fixtures;
using TUnit.Core.Exceptions;

namespace Hiero.Test.Integration.Network;

// Exercises the default channel factory's TLS certificate-hash pinning against a
// live network. TLS (port 50212) endpoints present self-signed certificates that
// standard validation cannot accept; the default factory pins them to the address
// book's published SHA-384 hash instead. These tests skip gracefully when the
// configured network exposes no usable TLS endpoint (e.g. a local Solo network
// whose address book omits 50212 / cert hashes).
public class TlsPinningTests
{
    private const int ProbeTimeoutMs = 5000;

    [Test]
    public async Task Discovered_Tls_Endpoints_Carry_Certificate_Hashes()
    {
        var endpoints = await DiscoverTlsEndpointsAsync();
        foreach (var endpoint in endpoints)
        {
            await Assert.That(endpoint.Uri.Scheme).IsEqualTo("https");
            await Assert.That(endpoint.Uri.Port).IsEqualTo(50212);
            await Assert.That(endpoint.CertificateHash.IsEmpty).IsFalse();
            // Address-book hashes are SHA-384 (48 bytes).
            await Assert.That(endpoint.CertificateHash.Length).IsEqualTo(48);
        }
    }

    [Test]
    public async Task Can_Query_Balance_Over_Pinned_Tls_Channel()
    {
        // Discovery itself pings each TLS endpoint through the default (pinning)
        // channel factory, so a responsive endpoint already proves the handshake
        // succeeds. Do a real paid-node-free balance query to confirm end-to-end.
        var endpoint = (await DiscoverTlsEndpointsAsync())[0];
        await using var client = TestNetwork.CreateClient(endpoint);
        var balance = await client.GetAccountBalanceAsync(TestNetwork.Payer);
        await Assert.That(balance > 0).IsTrue();
    }

    [Test]
    public async Task Wrong_Certificate_Hash_Is_Rejected()
    {
        var endpoint = (await DiscoverTlsEndpointsAsync())[0];
        // Corrupt the pinned hash so the presented certificate can no longer match.
        var corrupted = endpoint.CertificateHash.ToArray();
        corrupted[0] ^= 0xFF;
        var badEndpoint = new ConsensusNodeEndpoint(endpoint.Node, endpoint.Uri, corrupted);
        await using var client = TestNetwork.CreateClient(badEndpoint);
        // The TLS handshake fails validation, surfacing as a communication error.
        await Assert.That(async () =>
        {
            await client.GetAccountBalanceAsync(TestNetwork.Payer);
        }).ThrowsException();
    }

    private static async Task<ConsensusNodeEndpoint[]> DiscoverTlsEndpointsAsync()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var endpoints = (await mirror.GetActiveConsensusNodesAsync(ProbeTimeoutMs, ConsensusNodeTransport.Tls)).Keys.ToArray();
        if (endpoints.Length == 0)
        {
            throw new SkipTestException("No TLS (port 50212) consensus endpoint with a certificate hash responded on the configured network.");
        }
        return endpoints;
    }
}
