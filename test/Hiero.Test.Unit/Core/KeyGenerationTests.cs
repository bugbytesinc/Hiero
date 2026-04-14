// SPDX-License-Identifier: Apache-2.0
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Core;

public class KeyGenerationTests
{
    [Test]
    public async Task GenerateEd25519KeyPair_Produces_Valid_Signatory()
    {
        var (publicKey, privateKey) = Generator.Ed25519KeyPair();

        var signatory = new Signatory(privateKey);
        var endorsement = new Endorsement(publicKey);

        await Assert.That(endorsement.Type).IsEqualTo(KeyType.Ed25519);
        await Assert.That(publicKey.Length).IsGreaterThan(0);
        await Assert.That(privateKey.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task GenerateEd25519KeyPair_Produces_Unique_Keys()
    {
        var (pub1, priv1) = Generator.Ed25519KeyPair();
        var (pub2, priv2) = Generator.Ed25519KeyPair();

        await Assert.That(pub1.Span.SequenceEqual(pub2.Span)).IsFalse();
        await Assert.That(priv1.Span.SequenceEqual(priv2.Span)).IsFalse();
    }

    [Test]
    public async Task GenerateEcdsaSecp256k1KeyPair_Produces_Valid_Signatory()
    {
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();

        var signatory = new Signatory(privateKey);
        var endorsement = new Endorsement(publicKey);

        await Assert.That(endorsement.Type).IsEqualTo(KeyType.ECDSASecp256K1);
        await Assert.That(publicKey.Length).IsGreaterThan(0);
        await Assert.That(privateKey.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task GenerateEcdsaSecp256k1KeyPair_Produces_Unique_Keys()
    {
        var (pub1, priv1) = Generator.Secp256k1KeyPair();
        var (pub2, priv2) = Generator.Secp256k1KeyPair();

        await Assert.That(pub1.Span.SequenceEqual(pub2.Span)).IsFalse();
        await Assert.That(priv1.Span.SequenceEqual(priv2.Span)).IsFalse();
    }
}
