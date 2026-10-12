// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Hiero.Test.Helpers;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Hiero.Test.Unit.Contract;

/// <summary>
/// Signatures on EVM (RLP) transactions must be in the EIP-2 low-s form:
/// Hiero consensus nodes reject s > n/2 from v0.77 (EthTxSigs.extractSig),
/// as Ethereum has since Homestead. Raw ECDSA yields a high s about half the
/// time, so these tests sign many messages and check every result.
/// </summary>
public class EvmSignatureTests
{
    private static readonly Org.BouncyCastle.Asn1.X9.X9ECParameters Curve = SecNamedCurves.GetByName("secp256k1");

    [Test]
    public async Task Evm_Signatures_Are_Low_S_Valid_And_Carry_The_Right_Recovery_Id()
    {
        var (publicKeyDer, privateKeyDer) = Generator.Secp256k1KeyPair();
        var signatory = new Signatory(privateKeyDer);
        var publicKey = (ECPublicKeyParameters)PublicKeyFactory.CreateKey(publicKeyDer.ToArray());
        var halfN = Curve.N.ShiftRight(1);
        var verifier = new ECDsaSigner();
        verifier.Init(false, publicKey);

        for (var i = 0; i < 256; i++)
        {
            var message = Generator.KeyPair().publicKey.ToArray();
            var (rBytes, sBytes, recoveryId) = ((ISignatory)signatory).SignEvm(message);
            var r = new BigInteger(1, rBytes);
            var s = new BigInteger(1, sBytes);
            var hash = Keccak256(message);
            var e = new BigInteger(1, hash);

            await Assert.That(s.CompareTo(halfN) <= 0).IsTrue();
            await Assert.That(verifier.VerifySignature(hash, r, s)).IsTrue();
            await Assert.That(RecoveryIdOf(e, r, s, publicKey)).IsEqualTo(recoveryId);
        }
    }

    [Test]
    public async Task RlpEncoded_Transactions_Carry_A_Low_S_Signature()
    {
        var (_, privateKeyDer) = Generator.Secp256k1KeyPair();
        var signatory = new Signatory(privateKeyDer);
        var halfN = Curve.N.ShiftRight(1);
        for (var i = 0; i < 64; i++)
        {
            var encoded = new EvmTransactionInput
            {
                ToEvmAddress = new EvmAddress(Generator.KeyPair().publicKey.Span[^20..]),
                GasLimit = 50_000,
                ChainId = 296,
                EvmNonce = i
            }.RlpEncode(signatory);
            // A legacy transaction is the list [nonce, gasPrice, gasLimit, to, value, data, v, r, s].
            var fields = RlpListItems(encoded.Span);
            await Assert.That(fields.Count).IsEqualTo(9);
            var s = fields[8];
            await Assert.That(new BigInteger(1, s).CompareTo(halfN) <= 0).IsTrue();
        }
    }

    /// <summary>
    /// The y-parity of R, recomputed from a verified signature as
    /// u1·G + u2·Q (u1 = e/s, u2 = r/s): the recovery id it must carry.
    /// </summary>
    private static int RecoveryIdOf(BigInteger e, BigInteger r, BigInteger s, ECPublicKeyParameters publicKey)
    {
        var sInverse = s.ModInverse(Curve.N);
        var point = Curve.G.Multiply(e.Multiply(sInverse).Mod(Curve.N))
            .Add(publicKey.Q.Multiply(r.Multiply(sInverse).Mod(Curve.N)))
            .Normalize();
        return point.AffineYCoord.TestBitZero() ? 1 : 0;
    }

    private static byte[] Keccak256(byte[] data)
    {
        var digest = new KeccakDigest(256);
        digest.BlockUpdate(data, 0, data.Length);
        var hash = new byte[32];
        digest.DoFinal(hash, 0);
        return hash;
    }

    /// <summary>
    /// The byte-string items of a single top-level RLP list (enough for a
    /// legacy transaction, whose items are all strings).
    /// </summary>
    private static List<byte[]> RlpListItems(ReadOnlySpan<byte> encoded)
    {
        var (listStart, listLength) = RlpHeader(encoded, 0, 0xc0);
        var items = new List<byte[]>();
        var position = listStart;
        while (position < listStart + listLength)
        {
            if (encoded[position] < 0x80)
            {
                items.Add([encoded[position]]);
                position++;
                continue;
            }
            var (itemStart, itemLength) = RlpHeader(encoded, position, 0x80);
            items.Add(encoded.Slice(itemStart, itemLength).ToArray());
            position = itemStart + itemLength;
        }
        return items;
    }

    private static (int Start, int Length) RlpHeader(ReadOnlySpan<byte> encoded, int position, int baseOffset)
    {
        var prefix = encoded[position] - baseOffset;
        if (prefix <= 55)
        {
            return (position + 1, prefix);
        }
        var lengthOfLength = prefix - 55;
        var length = 0;
        for (var i = 1; i <= lengthOfLength; i++)
        {
            length = (length << 8) | encoded[position + i];
        }
        return (position + 1 + lengthOfLength, length);
    }
}
