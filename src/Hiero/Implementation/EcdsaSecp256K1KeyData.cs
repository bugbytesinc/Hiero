// SPDX-License-Identifier: Apache-2.0
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;

namespace Hiero.Implementation;

internal sealed class EcdsaSecp256K1KeyData
{
    internal readonly ECPrivateKeyParameters PrivateKey;
    internal readonly byte[] PublicKey;

    internal EcdsaSecp256K1KeyData(ECPrivateKeyParameters privateKey)
    {
        PrivateKey = privateKey;
        PublicKey = privateKey.Parameters.G.Multiply(privateKey.D).GetEncoded(true);
    }

    internal void Sign(IInvoice invoice)
    {
        var digest = new KeccakDigest(256);
        digest.BlockUpdate(invoice.TransactionBytes.Span);
        var hash = new byte[32];
        digest.DoFinal(hash, 0);
        var signer = new ECDsaSigner(new HMacDsaKCalculator(new Sha256Digest()));
        signer.Init(true, PrivateKey);
        var components = signer.GenerateSignature(hash);
        Span<byte> encoded = stackalloc byte[64];
        Insert256Int(components[0], encoded[..32]);
        Insert256Int(components[1], encoded[32..]);
        var prefix = PublicKey.AsSpan(0, Math.Min(Math.Max(6, invoice.MinimumDesiredPrefixSize), PublicKey.Length));
        invoice.AddSignature(KeyType.ECDSASecp256K1, prefix, encoded);
    }

    internal (byte[] R, byte[] S, int RecoveryId) SignEvm(ReadOnlySpan<byte> data)
    {
        var digest = new KeccakDigest(256);
        digest.BlockUpdate(data);
        var hash = new byte[32];
        digest.DoFinal(hash, 0);
        var signer = new ExtendedEcdsaSigner(new HMacDsaKCalculator(new Sha256Digest()));
        signer.Init(true, PrivateKey);
        var components = signer.GenerateSignatureWithRecoveryId(hash);
        var v = components[0];
        var r = components[1].ToByteArrayUnsigned();
        var s = components[2].ToByteArrayUnsigned();

        return (r, s, v.IntValue);
    }

    private static void Insert256Int(BigInteger component, Span<byte> destination)
    {
        Span<byte> bytes = stackalloc byte[component.GetLengthofByteArrayUnsigned()];
        component.ToByteArrayUnsigned(bytes);
        if (bytes.Length > 32)
        {
            bytes = bytes[^32..];
        }
        bytes.CopyTo(destination[^bytes.Length..]);
    }
}
