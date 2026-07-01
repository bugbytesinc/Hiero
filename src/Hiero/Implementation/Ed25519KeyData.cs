// SPDX-License-Identifier: Apache-2.0
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Hiero.Implementation;

internal sealed class Ed25519KeyData
{
    internal readonly Ed25519PrivateKeyParameters PrivateKey;
    internal readonly byte[] PublicKey;

    internal Ed25519KeyData(Ed25519PrivateKeyParameters privateKey)
    {
        PrivateKey = privateKey;
        PublicKey = privateKey.GeneratePublicKey().GetEncoded();
    }

    internal void Sign(IInvoice invoice)
    {
        var signer = new Ed25519Signer();
        signer.Init(true, PrivateKey);
        signer.BlockUpdate(invoice.TransactionBytes.Span);
        var signature = signer.GenerateSignature();
        signer.Reset();
        var prefix = PublicKey.AsSpan(0, Math.Min(Math.Max(6, invoice.MinimumDesiredPrefixSize), PublicKey.Length));
        invoice.AddSignature(KeyType.Ed25519, prefix, signature);
    }
}
