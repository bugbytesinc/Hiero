// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC.Rfc8032;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Hiero;
/// <summary>
/// Helper class to produce public and private key values
/// from mnemonic word phrases.
/// </summary>
/// <remarks>
/// Warning: this class has not been hardened against external attacks,
/// it keeps the root seed in memory.
/// </remarks>
public sealed class Mnemonic
{
    /// <summary>
    /// The Ed25519 DER Encoding prefix, for just the PRIVATE key.
    /// </summary>
    private static ReadOnlySpan<byte> _ed25519PrivateKeyDerPrefix =>
    [
        0x30, 0x2e, 0x02, 0x01, 0x00, 0x30, 0x05, 0x06,
        0x03, 0x2b, 0x65, 0x70, 0x04, 0x22, 0x04, 0x20
    ];
    /// <summary>
    /// The root seed key phrase for generating Ed25519 keys
    /// from a mnemonic seed phrase.
    /// </summary>
    private static ReadOnlySpan<byte> _ed25519SeedKey => "ed25519 seed"u8;
    /// <summary>
    /// The root seed key phrase for generating ECDSA Secp256k1 keys
    /// from a mnemonic seed phrase.
    /// </summary>
    private static ReadOnlySpan<byte> _ecdsaSecp256k1SeedKey => "Bitcoin seed"u8;
    /// <summary>
    /// The master seed value in bytes generated from the
    /// mnemonic words given to the constructor.  (words
    /// are not saved internally)
    /// </summary>
    private readonly ReadOnlyMemory<byte> _seed;
    /// <summary>
    /// Constructor taking an array of mnemonic words
    /// and a passphrase.
    /// </summary>
    /// <param name="words">
    /// An array of words that make up the mnemonic.
    /// </param>
    /// <param name="passphrase">
    /// Optional password (empty string or null is allowed 
    /// for no password).
    /// </param>
    public Mnemonic(string[] words, string passphrase)
    {
        var mnemonicBytes = Encoding.UTF8.GetBytes(string.Join(' ', words));
        var saltBytes = Encoding.UTF8.GetBytes("mnemonic" + (passphrase ?? ""));
        _seed = Rfc2898DeriveBytes.Pbkdf2(
            mnemonicBytes,
            saltBytes,
            2048,
            HashAlgorithmName.SHA512,
            64
        );
    }
    /// <summary>
    /// Computes the HD key pair for this Mnemonic.
    /// </summary>
    /// <param name="path">
    /// The key derivation path that should be used to
    /// generate the private and public key values.
    /// </param>
    /// <returns>
    /// DER Encoded public and private key values.
    /// </returns>
    public (ReadOnlyMemory<byte> publicKey, ReadOnlyMemory<byte> privateKey) GenerateKeyPair(KeyDerivationPath path)
    {
        if (path.KeyType == KeyType.Ed25519)
        {
            var keyDataAndChainCode = HMACSHA512.HashData(_ed25519SeedKey, _seed.Span);
            Span<byte> data = stackalloc byte[37];
            foreach (uint index in path.Path.Span)
            {
                // TODO - Review the spec for any path that is
                // not fully "hardened" to make sure this is the
                // correct key value to put here. Presently all
                // Ed25519 paths in the ecosystem are completely
                // "hardened".
                keyDataAndChainCode.AsSpan(0, 32).CopyTo(data[1..33]);
                BinaryPrimitives.WriteUInt32BigEndian(data[33..], index);
                keyDataAndChainCode = HMACSHA512.HashData(keyDataAndChainCode.AsSpan(32), data);
            }
            var keyParams = new Ed25519PrivateKeyParameters(keyDataAndChainCode, 0);
            var publicKey = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyParams.GeneratePublicKey()).GetDerEncoded();
            var privateKey = new byte[_ed25519PrivateKeyDerPrefix.Length + Ed25519.SecretKeySize];
            _ed25519PrivateKeyDerPrefix.CopyTo(privateKey);
            Array.Copy(keyParams.GetEncoded(), 0, privateKey, _ed25519PrivateKeyDerPrefix.Length, Ed25519.SecretKeySize);
            return (publicKey, privateKey);
        }
        else if (path.KeyType == KeyType.ECDSASecp256K1)
        {
            var keyDataAndChainCode = HMACSHA512.HashData(_ecdsaSecp256k1SeedKey, _seed.Span);
            foreach (uint index in path.Path.Span)
            {
                keyDataAndChainCode = CKDprivEcdsaSecp256k1(keyDataAndChainCode, index);
            }
            var privateKeyParams = new ECPrivateKeyParameters(new BigInteger(1, keyDataAndChainCode[..32]), KeyConstants.EcdsaSecp256k1DomainParams);
            var publicKeyParams = new ECPublicKeyParameters(privateKeyParams.Parameters.G.Multiply(privateKeyParams.D), privateKeyParams.Parameters);
            var privateKey = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKeyParams).GetDerEncoded();
            var publicKey = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKeyParams).GetDerEncoded();
            return (publicKey, privateKey);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(path), $"Key type of {path.KeyType} is not supported.");
        }
    }
    private static byte[] CKDprivEcdsaSecp256k1(byte[] parentKeyDataAndChainCode, uint index)
    {
        Span<byte> data = stackalloc byte[37];
        if ((index & 0x80000000) == 0x80000000)
        {
            parentKeyDataAndChainCode.AsSpan(0, 32).CopyTo(data[1..33]);
        }
        else
        {
            var parentPrivateKeyParams = new ECPrivateKeyParameters(new BigInteger(1, parentKeyDataAndChainCode[..32]), KeyConstants.EcdsaSecp256k1DomainParams);
            var parentPublicKeyParams = new ECPublicKeyParameters(parentPrivateKeyParams.Parameters.G.Multiply(parentPrivateKeyParams.D), parentPrivateKeyParams.Parameters);
            parentPublicKeyParams.Q.GetEncoded(true).AsSpan().CopyTo(data);
        }
        BinaryPrimitives.WriteUInt32BigEndian(data[33..], index);
        var digest = HMACSHA512.HashData(parentKeyDataAndChainCode.AsSpan(32), data);
        var digestLeft = new BigInteger(1, digest[..32]);
        var keyParam = new BigInteger(1, parentKeyDataAndChainCode[..32]);
        var keyBytes = digestLeft.Add(keyParam).Mod(KeyConstants.EcdsaSecp256k1DomainParams.N).ToByteArrayUnsigned();
        var childKeyDataAndChainCode = new byte[64];
        Array.Copy(keyBytes, 0, childKeyDataAndChainCode, 32 - keyBytes.Length, keyBytes.Length);
        Array.Copy(digest, 32, childKeyDataAndChainCode, 32, 32);
        return childKeyDataAndChainCode;
    }
}
