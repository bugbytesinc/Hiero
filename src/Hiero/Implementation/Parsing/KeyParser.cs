// SPDX-License-Identifier: Apache-2.0
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC.Rfc8032;
using Org.BouncyCastle.Security;
using System.Runtime.InteropServices;

namespace Hiero.Implementation.Parsing;

internal static class KeyParser
{
    internal static (KeyType keyType, object keyData) ParsePrivateKey(ReadOnlyMemory<byte> privateKey)
    {
        if (privateKey.Length == 32)
        {
            throw new ArgumentOutOfRangeException(nameof(privateKey), $"The private key byte length of 32 is ambiguous, unable to determine which type of key this refers to.");
        }
        // This is the "special" hedera encoding.
        if (privateKey.Length == 50 && privateKey.Span.StartsWith(KeyConstants.HederaSecp256k1PrivateKeyDerPrefix))
        {
            try
            {
                return (KeyType.ECDSASecp256K1, new EcdsaSecp256K1KeyData(CreateSecp256k1PrivateKey(privateKey.Slice(18, 32))));
            }
            catch (Exception ex) when (ex is not ArgumentOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException($"Expected the 50 byte length key to be a Hedera ECDSA Secp256k1 private key, it is not parsable as such.", ex);
            }
        }
        // Bouncy Castle Recognized DER Encodings
        AsymmetricKeyParameter asymmetricKeyParameter;
        try
        {
            asymmetricKeyParameter = CreatePrivateKey(privateKey);
        }
        catch (Exception ex)
        {
            throw new ArgumentOutOfRangeException("The private key does not appear to be encoded as a recognizable private key format.", ex);
        }
        if (asymmetricKeyParameter is Ed25519PrivateKeyParameters ed25519PrivateKeyParameters)
        {
            if (ed25519PrivateKeyParameters.IsPrivate)
            {
                return (KeyType.Ed25519, new Ed25519KeyData(ed25519PrivateKeyParameters));
            }
            throw new ArgumentOutOfRangeException(nameof(privateKey), "This is not an Ed25519 private key, it appears to be a public key.");
        }
        if (asymmetricKeyParameter is ECPrivateKeyParameters ecPrivateKeyParameters)
        {
            if (ecPrivateKeyParameters.IsPrivate)
            {
                if (IsSecp256k1(ecPrivateKeyParameters.Parameters))
                {
                    return (KeyType.ECDSASecp256K1, new EcdsaSecp256K1KeyData(ecPrivateKeyParameters));
                }
                throw new ArgumentOutOfRangeException(nameof(privateKey), "This is not an ECDSA Secp256K1 private key.");
            }
            throw new ArgumentOutOfRangeException(nameof(privateKey), "This is not an ECDSA Secp256K1 private key, it appears to be a public key.");
        }
        throw new ArgumentOutOfRangeException(nameof(privateKey), "The private key does not appear to be encoded in Ed25519 or ECDSA Secp256k1 format.");
    }
    internal static (KeyType keyType, object keyData) ParsePublicKey(ReadOnlyMemory<byte> publicKey)
    {
        if (publicKey.Length == Ed25519.PublicKeySize)
        {
            try
            {
                return (KeyType.Ed25519, new Ed25519EndorsementData(new Ed25519PublicKeyParameters(publicKey.Span)));
            }
            catch (Exception ex)
            {
                throw new ArgumentOutOfRangeException($"Expected the {Ed25519.PublicKeySize} byte length key to be an Ed25519 public key, it is not parsable as such.", ex);
            }
        }
        if (publicKey.Length == 33)
        {
            try
            {
                var q = KeyConstants.EcdsaSecp256k1Curve.Curve.DecodePoint(publicKey.Span);
                return (KeyType.ECDSASecp256K1, new EcdsaSecp256K1EndorsementData(new ECPublicKeyParameters(q, KeyConstants.EcdsaSecp256k1DomainParams)));
            }
            catch (Exception ex)
            {
                throw new ArgumentOutOfRangeException($"Expected the 33 byte length key to be an ECDSA Secp256k1 public key, it is not parsable as such.", ex);
            }
        }
        // This is the "special" hedera encoding.
        if (publicKey.Length == 47 && publicKey.Span.StartsWith(KeyConstants.HederaSecp256k1PublicKeyDerPrefix))
        {
            try
            {
                var q = KeyConstants.EcdsaSecp256k1Curve.Curve.DecodePoint(publicKey[14..].Span);
                return (KeyType.ECDSASecp256K1, new EcdsaSecp256K1EndorsementData(new ECPublicKeyParameters(q, KeyConstants.EcdsaSecp256k1DomainParams)));
            }
            catch (Exception ex)
            {
                throw new ArgumentOutOfRangeException($"Expected the 47 byte length key to be a Hedera ECDSA Secp256k1 public key, it is not parsable as such.", ex);
            }
        }
        // Bouncy Castle Recognized DER Encodings
        AsymmetricKeyParameter asymmetricKeyParameter;
        try
        {
            asymmetricKeyParameter = CreatePublicKey(publicKey);
        }
        catch (Exception ex)
        {
            throw new ArgumentOutOfRangeException("The public key does not appear to be encoded as a recognizable public key format.", ex);
        }
        if (asymmetricKeyParameter is Ed25519PublicKeyParameters ed25519PublicKeyParameters)
        {
            if (!ed25519PublicKeyParameters.IsPrivate)
            {
                return (KeyType.Ed25519, new Ed25519EndorsementData(ed25519PublicKeyParameters));
            }
            throw new ArgumentOutOfRangeException(nameof(publicKey), "This is not an Ed25519 public key, it appears to be a private key.");
        }
        if (asymmetricKeyParameter is ECPublicKeyParameters ecPublicKeyParameters)
        {
            if (!ecPublicKeyParameters.IsPrivate)
            {
                if (IsSecp256k1(ecPublicKeyParameters.Parameters))
                {
                    return (KeyType.ECDSASecp256K1, new EcdsaSecp256K1EndorsementData(ecPublicKeyParameters));
                }
                throw new ArgumentOutOfRangeException(nameof(publicKey), "This is not an ECDSA Secp256K1 public key.");
            }
            throw new ArgumentOutOfRangeException(nameof(publicKey), "This is not an ECDSA Secp256K1 public key, it appears to be a private key.");
        }
        throw new ArgumentOutOfRangeException(nameof(publicKey), "The public key does not appear to be encoded in Ed25519 or ECDSA Secp256K1 format.");
    }

    internal static Ed25519PrivateKeyParameters ParsePrivateEd25519Key(ReadOnlyMemory<byte> privateKey)
    {
        AsymmetricKeyParameter asymmetricKeyParameter;
        try
        {
            // Check to see if we have a raw key.
            if (privateKey.Length == Ed25519.SecretKeySize)
            {
                return new Ed25519PrivateKeyParameters(privateKey.Span);
            }
            asymmetricKeyParameter = CreatePrivateKey(privateKey);
        }
        catch (Exception ex)
        {
            if (privateKey.Length == 0)
            {
                throw new ArgumentOutOfRangeException("Private Key cannot be empty.", ex);
            }
            throw new ArgumentOutOfRangeException("The private key does not appear to be encoded as a recognizable Ed25519 format.", ex);
        }
        if (asymmetricKeyParameter is Ed25519PrivateKeyParameters ed25519PrivateKeyParameters)
        {
            if (ed25519PrivateKeyParameters.IsPrivate)
            {
                return ed25519PrivateKeyParameters;
            }
            throw new ArgumentOutOfRangeException(nameof(privateKey), "This is not an Ed25519 private key, it appears to be a public key.");
        }
        throw new ArgumentOutOfRangeException(nameof(privateKey), "The private key does not appear to be encoded in Ed25519 format.");
    }
    internal static Ed25519PublicKeyParameters ParsePublicEd25519Key(ReadOnlyMemory<byte> publicKey)
    {
        AsymmetricKeyParameter asymmetricKeyParameter;
        try
        {
            // Check to see if we have a raw key.   
            if (publicKey.Length == Ed25519.PublicKeySize)
            {
                return new Ed25519PublicKeyParameters(publicKey.Span);
            }
            // If not, assume it is DER encoded.
            asymmetricKeyParameter = CreatePublicKey(publicKey);
        }
        catch (Exception ex)
        {
            throw new ArgumentOutOfRangeException("The public key does not appear to be encoded in a recognizable Ed25519 format.", ex);
        }
        if (asymmetricKeyParameter is Ed25519PublicKeyParameters ed25519PublicKeyParameters)
        {
            if (!ed25519PublicKeyParameters.IsPrivate)
            {
                return ed25519PublicKeyParameters;
            }
            throw new ArgumentOutOfRangeException(nameof(publicKey), "This is not an Ed25519 public key, it appears to be a private key.");
        }
        throw new ArgumentOutOfRangeException(nameof(publicKey), "The public key does not appear to be encoded in a recognizable Ed25519 format.");
    }
    internal static ECPrivateKeyParameters ParsePrivateEcdsaSecp256k1Key(ReadOnlyMemory<byte> privateKey)
    {
        if (privateKey.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(privateKey), "Private Key cannot be empty.");
        }
        AsymmetricKeyParameter asymmetricKeyParameter;
        try
        {
            if (privateKey.Length > 30 && privateKey.Length < 34)
            {
                return CreateSecp256k1PrivateKey(privateKey);
            }
            // This is the "special" hedera encoding.
            if (privateKey.Length == 50 && privateKey.Span.StartsWith(KeyConstants.HederaSecp256k1PrivateKeyDerPrefix))
            {
                return CreateSecp256k1PrivateKey(privateKey.Slice(18, 32));
            }
            // Bouncy Castle Recognized DER Encodings
            asymmetricKeyParameter = CreatePrivateKey(privateKey);
        }
        catch (Exception ex) when (ex is not ArgumentOutOfRangeException)
        {
            throw new ArgumentOutOfRangeException("The private key was not provided in a recognizable ECDSA Secp256K1 format.", ex);
        }
        if (asymmetricKeyParameter is ECPrivateKeyParameters ecPrivateKeyParameters)
        {
            if (ecPrivateKeyParameters.IsPrivate)
            {
                if (IsSecp256k1(ecPrivateKeyParameters.Parameters))
                {
                    return ecPrivateKeyParameters;
                }
                throw new ArgumentOutOfRangeException(nameof(privateKey), "This is not an ECDSA Secp256K1 private key.");
            }
            throw new ArgumentOutOfRangeException(nameof(privateKey), "This is not an ECDSA Secp256K1 private key, it appears to be a public key.");
        }
        throw new ArgumentOutOfRangeException(nameof(privateKey), "The private key does not appear to be encoded in ECDSA Secp256K1 format.");
    }
    internal static ECPublicKeyParameters ParsePublicEcdsaSecp256k1Key(ReadOnlyMemory<byte> publicKey)
    {
        AsymmetricKeyParameter asymmetricKeyParameter;
        try
        {
            // First, check to see if we have a raw compressed key
            if (publicKey.Length == 33)
            {
                var q = KeyConstants.EcdsaSecp256k1Curve.Curve.DecodePoint(publicKey.Span);
                return new ECPublicKeyParameters(q, KeyConstants.EcdsaSecp256k1DomainParams);
            }
            // Or is this the "special" hedera encoding.
            if (publicKey.Length == 47 && publicKey.Span.StartsWith(KeyConstants.HederaSecp256k1PublicKeyDerPrefix))
            {
                var q = KeyConstants.EcdsaSecp256k1Curve.Curve.DecodePoint(publicKey[14..].Span);
                return new ECPublicKeyParameters(q, KeyConstants.EcdsaSecp256k1DomainParams);
            }
            // Bouncy Castle Recognized DER Encodings
            asymmetricKeyParameter = CreatePublicKey(publicKey);
        }
        catch (Exception ex)
        {
            throw new ArgumentOutOfRangeException("The public key was not provided in a recognizable ECDSA Secp256K1 format.", ex);
        }
        if (asymmetricKeyParameter is ECPublicKeyParameters ecPublicKeyParameters)
        {
            if (!ecPublicKeyParameters.IsPrivate)
            {
                if (IsSecp256k1(ecPublicKeyParameters.Parameters))
                {
                    return ecPublicKeyParameters;
                }
                throw new ArgumentOutOfRangeException(nameof(publicKey), "This is not an ECDSA Secp256K1 public key.");
            }
            throw new ArgumentOutOfRangeException(nameof(publicKey), "This is not an ECDSA Secp256K1 public key, it appears to be a private key.");
        }
        throw new ArgumentOutOfRangeException(nameof(publicKey), "The public key was not provided in a recognizable ECDSA Secp256K1 format.");
    }
    private static AsymmetricKeyParameter CreatePrivateKey(ReadOnlyMemory<byte> key)
    {
        if (MemoryMarshal.TryGetArray(key, out ArraySegment<byte> segment))
        {
            using var stream = new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false);
            return PrivateKeyFactory.CreateKey(stream);
        }
        return PrivateKeyFactory.CreateKey(key.ToArray());
    }
    private static ECPrivateKeyParameters CreateSecp256k1PrivateKey(ReadOnlyMemory<byte> privateKey)
    {
        var d = new BigInteger(1, privateKey.Span);
        if (d.SignValue <= 0 || d.CompareTo(KeyConstants.EcdsaSecp256k1DomainParams.N) >= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(privateKey), "Invalid secp256k1 private key scalar.");
        }
        return new ECPrivateKeyParameters(d, KeyConstants.EcdsaSecp256k1DomainParams);
    }
    private static bool IsSecp256k1(ECDomainParameters parameters)
    {
        var expected = KeyConstants.EcdsaSecp256k1DomainParams;
        return parameters.Curve.Equals(expected.Curve) &&
               parameters.G.Equals(expected.G) &&
               parameters.N.Equals(expected.N) &&
               parameters.H.Equals(expected.H);
    }
    private static AsymmetricKeyParameter CreatePublicKey(ReadOnlyMemory<byte> key)
    {
        if (MemoryMarshal.TryGetArray(key, out ArraySegment<byte> segment))
        {
            using var stream = new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false);
            return PublicKeyFactory.CreateKey(stream);
        }
        return PublicKeyFactory.CreateKey(key.ToArray());
    }
}
