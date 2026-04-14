// SPDX-License-Identifier: Apache-2.0
using Hiero;
using Hiero.Implementation;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC.Rfc8032;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using System.Security.Cryptography;

namespace Hiero.Test.Helpers;

/// <summary>
/// Generates random Ed25519 and ECDSA Secp256k1 key pairs
/// for use in unit and integration tests.
/// </summary>
public static class KeyGeneration
{
    private static readonly ReadOnlyMemory<byte> Ed25519PrivateKeyDerPrefix =
        Hex.ToBytes("302e020100300506032b657004220420");

    /// <summary>
    /// Generates a random Ed25519 key pair, returning DER-encoded
    /// public and private keys ready for Endorsement/Signatory construction.
    /// </summary>
    public static (ReadOnlyMemory<byte> PublicKey, ReadOnlyMemory<byte> PrivateKey) GenerateEd25519KeyPair()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var keyParams = new Ed25519PrivateKeyParameters(randomBytes, 0);
        var publicKey = SubjectPublicKeyInfoFactory
            .CreateSubjectPublicKeyInfo(keyParams.GeneratePublicKey())
            .GetDerEncoded();
        var privateKey = new byte[Ed25519PrivateKeyDerPrefix.Length + Ed25519.SecretKeySize];
        Ed25519PrivateKeyDerPrefix.Span.CopyTo(privateKey);
        keyParams.GetEncoded().CopyTo(privateKey.AsSpan(Ed25519PrivateKeyDerPrefix.Length));
        return (publicKey, privateKey);
    }

    /// <summary>
    /// Generates a random ECDSA Secp256k1 key pair, returning DER-encoded
    /// public and private keys ready for Endorsement/Signatory construction.
    /// </summary>
    public static (ReadOnlyMemory<byte> PublicKey, ReadOnlyMemory<byte> PrivateKey) GenerateEcdsaSecp256k1KeyPair()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var privateKeyParams = new ECPrivateKeyParameters(
            new BigInteger(1, randomBytes),
            KeyUtils.EcdsaSecp256k1DomainParams);
        var publicKeyParams = new ECPublicKeyParameters(
            privateKeyParams.Parameters.G.Multiply(privateKeyParams.D),
            privateKeyParams.Parameters);
        var privateKey = PrivateKeyInfoFactory
            .CreatePrivateKeyInfo(privateKeyParams)
            .GetDerEncoded();
        var publicKey = SubjectPublicKeyInfoFactory
            .CreateSubjectPublicKeyInfo(publicKeyParams)
            .GetDerEncoded();
        return (publicKey, privateKey);
    }
}
