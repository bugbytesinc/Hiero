// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class ConsensusNodeCertHashParseTests
{
    [Test]
    public async Task Parses_Value_With_0x_Prefix()
    {
        var ok = ConsensusNodeDataExtensions.TryParseCertificateHash("0xeadd72fc", out var hash);
        await Assert.That(ok).IsTrue();
        await Assert.That(hash.Span.SequenceEqual(new byte[] { 0xea, 0xdd, 0x72, 0xfc })).IsTrue();
    }

    [Test]
    public async Task Parses_Value_Without_Prefix()
    {
        var ok = ConsensusNodeDataExtensions.TryParseCertificateHash("EADD72FC", out var hash);
        await Assert.That(ok).IsTrue();
        await Assert.That(hash.Span.SequenceEqual(new byte[] { 0xea, 0xdd, 0x72, 0xfc })).IsTrue();
    }

    [Test]
    public async Task Parses_Real_Length_Sha384_Value()
    {
        // 96 hex chars (with 0x prefix) → 48 bytes, matching a mainnet node_cert_hash.
        var value = "0xeadd72fcf60fab34228c729a6d2584ad6542c2e4e785a351d31ec83250d482d40dea6177fcdbcd6e01acb3e80c9b0ca8";
        var ok = ConsensusNodeDataExtensions.TryParseCertificateHash(value, out var hash);
        await Assert.That(ok).IsTrue();
        await Assert.That(hash.Length).IsEqualTo(48);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    [Arguments("0x")]
    [Arguments("eadd72f")]   // odd length
    [Arguments("zzzz")]      // non-hex
    public async Task Rejects_Unusable_Values(string? value)
    {
        var ok = ConsensusNodeDataExtensions.TryParseCertificateHash(value, out var hash);
        await Assert.That(ok).IsFalse();
        await Assert.That(hash.IsEmpty).IsTrue();
    }
}
