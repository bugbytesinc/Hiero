// SPDX-License-Identifier: Apache-2.0
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.X509;

namespace Hiero.Implementation;

internal sealed class EcdsaSecp256K1EndorsementData
{
    internal readonly ECPublicKeyParameters PublicKey;
    internal readonly byte[] RawPublicKey;

    internal EcdsaSecp256K1EndorsementData(ECPublicKeyParameters publicKey)
    {
        PublicKey = publicKey;
        RawPublicKey = publicKey.Q.GetEncoded(true);
    }

    internal ReadOnlyMemory<byte> EncodeAsDer()
    {
        return SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(PublicKey).GetDerEncoded();
    }

    internal ReadOnlyMemory<byte> EncodeAsHedera()
    {
        var prefix = KeyConstants.HederaSecp256k1PublicKeyDerPrefix;
        var result = new byte[prefix.Length + RawPublicKey.Length];
        prefix.CopyTo(result);
        RawPublicKey.CopyTo(result.AsSpan(prefix.Length));
        return result;
    }

    internal ReadOnlyMemory<byte> EncodeAsRaw()
    {
        return RawPublicKey;
    }

    internal bool Verify(ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> signature)
    {
        var digest = new KeccakDigest(256);
        digest.BlockUpdate(data.Span);
        var hash = new byte[digest.GetDigestSize()];
        digest.DoFinal(hash, 0);
        var signer = new ECDsaSigner(new HMacDsaKCalculator(new Sha256Digest()));
        signer.Init(false, PublicKey);
        var signatureSpan = signature.Span;
        var r = new BigInteger(1, signatureSpan[..32]);
        var s = new BigInteger(1, signatureSpan[32..]);
        return signer.VerifySignature(hash, r, s);
    }
}
