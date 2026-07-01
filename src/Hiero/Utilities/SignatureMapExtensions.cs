// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;

/// <summary>
/// Extension methods for working with protobuf signature maps.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SignatureMapExtensions
{
    /// <summary>
    /// Adds one or more signatures from the given <see cref="Signatory"/> to the
    /// existing <see cref="SignatureMap"/> instance.
    /// </summary>
    /// <remarks>
    /// This method does not examine the message being signed nor provide any
    /// validation of the results or de-duplication of identical signatures.
    /// It is meant to support edge cases outside the normal usage patterns of 
    /// this SDK, therefore use sparingly and with care.
    /// </remarks>
    /// <param name="signatureMap">
    /// The signature map which may or may not contain pre-existing signatures.
    /// </param>
    /// <param name="data">
    /// The message data to sign, does not necessarily need to be a transaction,
    /// and as a result, the TransactionId property will always be <code>None</code> and
    /// the message will be an empty string.
    /// </param>
    /// <param name="signatory">
    /// The signatory that is asked to sign the message.
    /// </param>
    public static async Task AddSignatureAsync(this SignatureMap signatureMap, ReadOnlyMemory<byte> data, Signatory signatory)
    {
        await ((ISignatory)signatory).SignAsync(new UncheckedSignatureMapInvoice(signatureMap, data)).ConfigureAwait(false);
    }
    /// <summary>
    /// Lightweight <see cref="IInvoice"/> implementation over an existing
    /// protobuf <see cref="SignatureMap"/> object, performs no validation
    /// checking, use with care.
    /// </summary>
    private class UncheckedSignatureMapInvoice : IInvoice
    {
        private readonly SignatureMap _signatureMap;
        private readonly ReadOnlyMemory<byte> _data;
        public TransactionId TransactionId => TransactionId.None;
        public string Memo => string.Empty;
        public ReadOnlyMemory<byte> TransactionBytes => _data;
        public int MinimumDesiredPrefixSize => int.MaxValue;
        public CancellationToken CancellationToken => default;

        public UncheckedSignatureMapInvoice(SignatureMap signatureMap, ReadOnlyMemory<byte> data)
        {
            _signatureMap = signatureMap;
            _data = data;
        }

        public void AddSignature(KeyType type, ReadOnlySpan<byte> publicPrefix, ReadOnlySpan<byte> signature)
        {
            var pair = new SignaturePair { PubKeyPrefix = ByteString.CopyFrom(publicPrefix) };
            var value = ByteString.CopyFrom(signature);
            switch (type)
            {
                case KeyType.Ed25519:
                    pair.Ed25519 = value;
                    break;
                case KeyType.ECDSASecp256K1:
                    pair.ECDSASecp256K1 = value;
                    break;
                case KeyType.Contract:
                    pair.Contract = value;
                    break;
            }
            _signatureMap.SigPair.Add(pair);
        }
    }
    /// <summary>
    /// Returns <code>True</code> if the given signature map contains
    /// enough correct signatures to satisfy the key signing requirements
    /// of the given <see cref="Endorsement"/>.
    /// </summary>
    /// <remarks>
    /// Note: this method does not return errors when it discovers invalid
    /// signatures.  It may still return <code>true</code> if sufficient
    /// correct signatures exist in the signature map to satisfy the 
    /// requirements described in the target <see cref="Endorsement"/>.
    /// </remarks>
    /// <param name="signatureMap">
    /// A signature map containing one or more signatures.
    /// </param>
    /// <param name="data">
    /// The message that was signed.
    /// </param>
    /// <param name="endorsement">
    /// The key signing requirements that must be met.
    /// </param>
    /// <returns>
    /// <code>True</code> if the signature map contains enough valid
    /// signatures for the data to satisfy the <see cref="Endorsement"/>
    /// key signing requirements.
    /// </returns>
    public static bool Satisfies(this SignatureMap signatureMap, ReadOnlyMemory<byte> data, Endorsement endorsement)
    {
        return isEndorsementSatisfied(endorsement);

        bool isEndorsementSatisfied(Endorsement endorsement)
        {
            return endorsement.Type switch
            {
                KeyType.Ed25519 => isKeySatisfied(endorsement),
                KeyType.ECDSASecp256K1 => isKeySatisfied(endorsement),
                KeyType.List => isListSatisfied(endorsement.RequiredCount, endorsement.List),
                _ => false
            };
        }

        bool isKeySatisfied(Endorsement endorsement)
        {
            var pairs = signatureMap.SigPair;
            if (pairs is null || pairs.Count == 0)
            {
                return false;
            }
            switch (endorsement.Type)
            {
                case KeyType.Ed25519:
                    var ed25519PublicKey = ((Ed25519EndorsementData)endorsement._data).RawPublicKey;
                    foreach (var p in pairs)
                    {
                        if (p.SignatureCase == SignaturePair.SignatureOneofCase.Ed25519 && ed25519PublicKey.StartsWith(p.PubKeyPrefix.Span))
                        {
                            if (((Ed25519EndorsementData)endorsement._data).Verify(data, p.Ed25519.Memory))
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                case KeyType.ECDSASecp256K1:
                    var ecdsaPublicKey = ((EcdsaSecp256K1EndorsementData)endorsement._data).RawPublicKey;
                    foreach (var p in pairs)
                    {
                        if (p.SignatureCase == SignaturePair.SignatureOneofCase.ECDSASecp256K1 && ecdsaPublicKey.StartsWith(p.PubKeyPrefix.Span))
                        {
                            if (endorsement.Verify(data, p.ECDSASecp256K1.Memory))
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                default:
                    return false;
            }
        }

        bool isListSatisfied(uint threshold, Endorsement[] list)
        {
            uint satisfied = 0;
            foreach (var endorsement in list)
            {
                if (isEndorsementSatisfied(endorsement))
                {
                    satisfied++;
                }
                if (satisfied >= threshold)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
