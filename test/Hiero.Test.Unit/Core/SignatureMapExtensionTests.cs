// SPDX-License-Identifier: Apache-2.0
using Hiero.Test.Helpers;
using Proto;

namespace Hiero.Test.Unit.Core;

public class SignatureMapExtensionTests
{
    [Test]
    public async Task Can_Add_Ed25519_Signature_To_Empty_Signature_Map()
    {
        var (publicKey, privateKey) = Generator.Ed25519KeyPair();

        var endorsement = new Endorsement(publicKey);
        var signatory = new Signatory(privateKey);
        var message = Generator.Secp256k1KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);

        await Assert.That(sigMap.SigPair).Count().IsEqualTo(1);
        var sigPair = sigMap.SigPair[0];
        await Assert.That(sigPair.PubKeyPrefix.Memory.ToArray().SequenceEqual(endorsement.ToBytes(KeyFormat.Raw).ToArray())).IsTrue();
        await Assert.That(sigPair.Ed25519).IsNotNull();
        await Assert.That(endorsement.Verify(message, sigPair.Ed25519.Memory)).IsTrue();
    }

    [Test]
    public async Task Can_Add_ECDSASecp256K1_Signature_To_Empty_Signature_Map()
    {
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();

        var endorsement = new Endorsement(publicKey);
        var signatory = new Signatory(privateKey);
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);

        await Assert.That(sigMap.SigPair).Count().IsEqualTo(1);
        var sigPair = sigMap.SigPair[0];
        await Assert.That(sigPair.PubKeyPrefix.Memory.ToArray().SequenceEqual(endorsement.ToBytes(KeyFormat.Raw).ToArray())).IsTrue();
        await Assert.That(sigPair.ECDSASecp256K1).IsNotNull();
        await Assert.That(endorsement.Verify(message, sigPair.ECDSASecp256K1.Memory)).IsTrue();
    }

    [Test]
    public async Task Can_Add_Multiple_Signatures()
    {
        var count = 10;
        var sigMap = new SignatureMap();
        var message = Generator.Secp256k1KeyPair().publicKey;
        for (var i = 0; i < count; i++)
        {
            var (_, privateKey) = Generator.KeyPair();
            var signatory = new Signatory(privateKey);
            await sigMap.AddSignatureAsync(message, signatory);
        }
        await Assert.That(sigMap.SigPair).IsNotNull();
        await Assert.That(sigMap.SigPair.Count).IsEqualTo(count);
    }

    [Test]
    public async Task Can_Satisfy_Two_Signatures()
    {
        var (publicKey1, privateKey1) = Generator.KeyPair();
        var (publicKey2, privateKey2) = Generator.KeyPair();

        var endorsement = new Endorsement(new Endorsement(publicKey1), new Endorsement(publicKey2));
        var signatory = new Signatory(new Signatory(privateKey1), new Signatory(privateKey2));
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);

        await Assert.That(sigMap.Satisfies(message, endorsement)).IsTrue();
    }

    [Test]
    public async Task Cannot_Satisfy_When_Missing_A_Signature()
    {
        var (publicKey1, privateKey1) = Generator.KeyPair();
        var (publicKey2, _) = Generator.KeyPair();

        var endorsement = new Endorsement(new Endorsement(publicKey1), new Endorsement(publicKey2));
        var signatory = new Signatory(privateKey1);
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);

        await Assert.That(sigMap.Satisfies(message, endorsement)).IsFalse();
    }

    [Test]
    public async Task Can_Satisfy_Complex_Key_Requirements()
    {
        var (publicKey1, privateKey1) = Generator.KeyPair();
        var (publicKey2, privateKey2) = Generator.KeyPair();
        var (publicKey3, privateKey3) = Generator.KeyPair();
        var (publicKey4, privateKey4) = Generator.KeyPair();
        var (publicKey5, privateKey5) = Generator.KeyPair();

        var endorsement = new Endorsement(
            new Endorsement(new Endorsement(publicKey1), new Endorsement(publicKey2)),
            new Endorsement(1, new Endorsement(publicKey3), new Endorsement(publicKey4)),
            new Endorsement(publicKey5));
        var signatory = new Signatory(
            new Signatory(privateKey1),
            new Signatory(privateKey2),
            new Signatory(privateKey3),
            new Signatory(privateKey4),
            new Signatory(privateKey5));
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);

        await Assert.That(sigMap.Satisfies(message, endorsement)).IsTrue();
    }

    [Test]
    public async Task Can_Fail_Complex_Key_Requirements()
    {
        var (publicKey1, privateKey1) = Generator.KeyPair();
        var (publicKey2, privateKey2) = Generator.KeyPair();
        var (publicKey3, privateKey3) = Generator.KeyPair();
        var (publicKey4, privateKey4) = Generator.KeyPair();
        var (publicKey5, _) = Generator.KeyPair();

        var endorsement = new Endorsement(
            new Endorsement(new Endorsement(publicKey1), new Endorsement(publicKey2)),
            new Endorsement(1, new Endorsement(publicKey3), new Endorsement(publicKey4)),
            new Endorsement(publicKey5));
        var signatory = new Signatory(
            new Signatory(privateKey1),
            new Signatory(privateKey2),
            new Signatory(privateKey3),
            new Signatory(privateKey4));
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);

        await Assert.That(sigMap.Satisfies(message, endorsement)).IsFalse();
    }

    [Test]
    public async Task Empty_Signature_Map_Does_Not_Satisfy()
    {
        var (publicKey, _) = Generator.KeyPair();
        var endorsement = new Endorsement(publicKey);
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();

        await Assert.That(sigMap.Satisfies(message, endorsement)).IsFalse();
    }

    [Test]
    public async Task Can_Satisfy_Single_Ed25519_Key()
    {
        var (publicKey, privateKey) = Generator.Ed25519KeyPair();

        var endorsement = new Endorsement(publicKey);
        var signatory = new Signatory(privateKey);
        var message = Generator.Secp256k1KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);

        await Assert.That(sigMap.Satisfies(message, endorsement)).IsTrue();
    }

    [Test]
    public async Task Can_Satisfy_Single_Secp256K1_Key()
    {
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();

        var endorsement = new Endorsement(publicKey);
        var signatory = new Signatory(privateKey);
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);

        await Assert.That(sigMap.Satisfies(message, endorsement)).IsTrue();
    }

    [Test]
    public async Task Wrong_Message_Does_Not_Satisfy()
    {
        var (publicKey, privateKey) = Generator.KeyPair();

        var endorsement = new Endorsement(publicKey);
        var signatory = new Signatory(privateKey);
        var signedMessage = Generator.Ed25519KeyPair().publicKey;
        var differentMessage = Generator.Secp256k1KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(signedMessage, signatory);

        await Assert.That(sigMap.Satisfies(differentMessage, endorsement)).IsFalse();
    }

    [Test]
    public async Task Can_Satisfy_Threshold_With_Partial_Signatures()
    {
        var (publicKey1, privateKey1) = Generator.KeyPair();
        var (publicKey2, _) = Generator.KeyPair();
        var (publicKey3, _) = Generator.KeyPair();

        var endorsement = new Endorsement(1, new Endorsement(publicKey1), new Endorsement(publicKey2), new Endorsement(publicKey3));
        var signatory = new Signatory(privateKey1);
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);

        await Assert.That(sigMap.Satisfies(message, endorsement)).IsTrue();
    }

    [Test]
    public async Task Satisfies_Ignores_Invalid_Signatures()
    {
        var (publicKey1, privateKey1) = Generator.KeyPair();
        var (publicKey2, privateKey2) = Generator.KeyPair();

        var endorsement = new Endorsement(new Endorsement(publicKey1), new Endorsement(publicKey2));
        var signatory = new Signatory(new Signatory(privateKey1), new Signatory(privateKey2));
        var message = Generator.Ed25519KeyPair().publicKey;
        var sigMap = new SignatureMap();
        await sigMap.AddSignatureAsync(message, signatory);
        for (var i = 0; i < 10; i++)
        {
            await sigMap.AddSignatureAsync(message, new Signatory(Generator.KeyPair().privateKey));
        }

        await Assert.That(sigMap.Satisfies(message, endorsement)).IsTrue();
    }
}
