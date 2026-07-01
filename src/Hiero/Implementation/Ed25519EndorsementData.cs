// SPDX-License-Identifier: Apache-2.0
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.X509;
using System.Runtime.InteropServices;

namespace Hiero.Implementation;

internal sealed class Ed25519EndorsementData
{
    internal readonly Ed25519PublicKeyParameters PublicKey;
    internal readonly byte[] RawPublicKey;

    internal Ed25519EndorsementData(Ed25519PublicKeyParameters publicKey)
    {
        PublicKey = publicKey;
        RawPublicKey = publicKey.GetEncoded();
    }

    internal ReadOnlyMemory<byte> EncodeAsDer()
    {
        return SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(PublicKey).GetDerEncoded();
    }

    internal ReadOnlyMemory<byte> EncodeAsRaw()
    {
        return RawPublicKey;
    }

    internal bool Verify(ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> signature)
    {
        var signer = new Ed25519Signer();
        signer.Init(false, PublicKey);
        signer.BlockUpdate(data.Span);
        return signer.VerifySignature(GetExactArrayOrCopy(signature));
    }

    private static byte[] GetExactArrayOrCopy(ReadOnlyMemory<byte> signature)
    {
        return MemoryMarshal.TryGetArray(signature, out var segment) &&
            segment.Offset == 0 &&
            segment.Count == segment.Array!.Length
            ? segment.Array
            : signature.ToArray();
    }
}
