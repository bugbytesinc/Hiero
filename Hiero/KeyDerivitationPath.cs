namespace Hiero;
/// <summary>
/// Enumerates known HD Key Derivation paths for
/// various Key formats and Wallets in the Hedera ecosystem.
/// </summary>
public sealed class KeyDerivationPath
{
    /// <summary>
    /// The Ed25519 Key Derivation matching the 24 word seed
    /// phrase as implemented by HashPack.
    /// </summary>
    public static readonly KeyDerivationPath HashPack = new KeyDerivationPath(KeyType.Ed25519, 44 | 0x80000000, 3030 | 0x80000000, 0 | 0x80000000, 0 | 0x80000000, 0 | 0x80000000);
    /// <summary>
    /// The Ed25519 Key Derivation matching the 12 word seed
    /// phrase as implemented by HashPack.
    /// </summary>
    public static readonly KeyDerivationPath Calaxy = new KeyDerivationPath(KeyType.Ed25519, 44 | 0x80000000, 3030 | 0x80000000, 0 | 0x80000000, 0 | 0x80000000, 0 | 0x80000000);
    /// <summary>
    /// The Ed25519 Key Derivation matching the 12 or 24 word seed
    /// phrase as implemented by Walla Wallet.
    /// </summary>
    public static readonly KeyDerivationPath WallaWallet = new KeyDerivationPath(KeyType.Ed25519, 44 | 0x80000000, 3030 | 0x80000000, 0 | 0x80000000, 0 | 0x80000000, 0 | 0x80000000);
    /// <summary>
    /// The ECDSA Secp256k1 Key Derivation matching the 12 word seed
    /// phrase as implemented by the Blade Wallet as of May 2023.
    /// </summary>
    public static readonly KeyDerivationPath Blade = new KeyDerivationPath(KeyType.ECDSASecp256K1, 44, 3030, 0, 0);
    /// <summary>
    /// The individual steps in the key derivation path 
    /// required to recreate keys from a mnemonic seed.
    /// </summary>
    public ReadOnlyMemory<uint> Path { get; private init; }
    /// <summary>
    /// The key type that should be produced when following
    /// this specific path.
    /// </summary>
    public KeyType KeyType { get; private init; }
    /// <summary>
    /// Constructor taking the various expected key derivation
    /// path components as input.
    /// </summary>
    /// <param name="keyType">
    /// The type of key that this derivation path is designed
    /// to generate.
    /// </param>
    /// <param name="path">
    /// The unsigned int values comprising of the derivation path, 
    /// for example m/44'/3030'/0'/0'/0'
    /// </param>
    private KeyDerivationPath(KeyType keyType, params uint[] path)
    {
        if (keyType != KeyType.Ed25519 && keyType != KeyType.ECDSASecp256K1)
        {
            throw new ArgumentOutOfRangeException(nameof(keyType), $"Key type of {keyType} is not supported.");
        }
        if (path == null || path.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(path), $"Path must include at least one value.");
        }
        KeyType = keyType;
        Path = path;
    }
}
