// SPDX-License-Identifier: Apache-2.0
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Hiero.Test.Unit.Core;

public class CertificateHashPinningTests
{
    [Test]
    public async Task ComputeCertHash_Matches_Sha384_Of_Pem_Plus_Trailing_Newline()
    {
        // This locks in the exact address-book convention verified empirically
        // against a live node: node_cert_hash == SHA-384 of the certificate's
        // standard PEM text WITH exactly one trailing newline. Getting the
        // newline (or DER-vs-PEM) wrong silently breaks trust for every node.
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=00000031", key, HashAlgorithmName.SHA256);
        using var cert = request.CreateSelfSigned(
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2040, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var expected = SHA384.HashData(Encoding.ASCII.GetBytes(cert.ExportCertificatePem() + "\n"));
        var actual = ConsensusClient.ComputeCertHash(cert);

        await Assert.That(actual.Length).IsEqualTo(48);
        await Assert.That(actual.SequenceEqual(expected)).IsTrue();
    }

    [Test]
    public async Task ComputeCertHash_Does_Not_Match_Pem_Without_Newline()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=00000031", key, HashAlgorithmName.SHA256);
        using var cert = request.CreateSelfSigned(
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2040, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var withoutNewline = SHA384.HashData(Encoding.ASCII.GetBytes(cert.ExportCertificatePem()));
        var actual = ConsensusClient.ComputeCertHash(cert);

        await Assert.That(actual.SequenceEqual(withoutNewline)).IsFalse();
    }
}
