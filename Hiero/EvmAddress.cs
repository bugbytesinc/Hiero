using Hiero.Converters;
using Hiero.Implementation;
using Org.BouncyCastle.Crypto.Digests;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

namespace Hiero;
/// <summary>
/// Represents 20-byte EVM Address for Hedera Virtual Machine.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
[JsonConverter(typeof(EvmAddressConverter))]
public sealed record EvmAddress : IEquatable<EvmAddress>
{
    /// <summary>
    /// Internal storage for the 20-byte EVM Address.
    /// </summary>
    private readonly byte[] _bytes = new byte[20];
    /// <summary>
    /// The 20-byte EVM Address of the contract.
    /// 
    /// Every contract has an EVM Address determined by its<code>shard.realm.num</code> id.
    /// 
    /// This Address is as follows:
    /// 
    ///     The first 4 bytes are the big-endian representation of the shard.
    ///     The next 8 bytes are the big-endian representation of the realm.
    ///     The final 8 bytes are the big-endian representation of the number.
    ///     
    /// In the above format, the shard and realm should match the encoded values.
    /// 
    /// Contracts created via CREATE2 have an <b>additional, primary address</b> that is 
    /// derived from the<a href="https://eips.ethereum.org/EIPS/eip-1014"> EIP-1014</a> 
    /// specification, and does not have a simple relation to a<tt> shard.realm.num</tt> id.
    /// (therefore shard and realm values do not match the encoded bytes)
    /// </summary>
    public ReadOnlySpan<byte> Bytes => _bytes;
    /// <summary>
    /// A special designation of an alias that can't be created.
    /// It represents the absence of a valid alias.  The network will
    /// interpret as "no account/file/topic/token/contract" when applied 
    /// to change parameters. (typically the value null is interpreted 
    /// as "make no change"). In this way, it is possible to remove a 
    /// auto-renew account from a topic.
    /// </summary>
    public static EvmAddress None { get; } = new EvmAddress(new byte[20].AsSpan());
    /// <summary>
    /// Constructor from ECDSASecp256K1 Endorsement, converts the public
    /// key into the appropriate 20-byte public key hash.
    /// <param name="endorsement">
    /// An ECDSASecp256K1 public key.  The EvmAddress will automatically 
    /// convert the public key into the matching 20-byte eth hash.
    /// </param>
    public EvmAddress(Endorsement endorsement)
    {
        if (endorsement.Type != KeyType.ECDSASecp256K1)
        {
            throw new ArgumentException("Can only compute a EvmAddress from an Endorsement of type ECDSASecp256K1.");
        }
        var publicKey = KeyUtils.ParsePublicEcdsaSecp256k1Key(endorsement.ToBytes(KeyFormat.Raw)).Q.GetEncoded(false);
        var digest = new KeccakDigest(256);
        digest.BlockUpdate(publicKey, 1, publicKey.Length - 1);
        byte[] hash = new byte[32];
        digest.DoFinal(hash, 0);
        hash.AsSpan(12, 20).CopyTo(_bytes);
    }
    /// <summary>
    /// Public Constructor, a <code>EvmAddress</code> is immutable after
    /// construction.
    /// </summary>
    /// <param name="bytes">
    /// The bytes representing the address, if originates from
    /// a long-zero address (shard.realm.num) form the encoding follows: 
    /// 
    ///     The first 4 bytes are the big-endian representation of the shard.
    ///     The next 8 bytes are the big-endian representation of the realm.
    ///     The final 8 bytes are the big-endian representation of the number.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// when any of the inputs are out of range, such as the bytes not having
    /// a length of 20.
    /// </exception>
    public EvmAddress(ReadOnlyMemory<byte> bytes) : this(bytes.Span)
    {
    }
    /// <summary>
    /// Public Constructor, a <code>EvmAddress</code> is immutable after
    /// construction.
    /// </summary>
    /// <param name="bytes">
    /// The bytes representing the address, if originates from
    /// a long-zero address (shard.realm.num) form the encoding follows: 
    /// 
    ///     The first 4 bytes are the big-endian representation of the shard.
    ///     The next 8 bytes are the big-endian representation of the realm.
    ///     The final 8 bytes are the big-endian representation of the number.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// when any of the inputs are out of range, such as the bytes not having
    /// a length of 20.
    /// </exception>
    public EvmAddress(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 20)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "The encoded bytes must have a length of 20.");
        }
        bytes.CopyTo(_bytes);
    }
    /// <summary>
    /// Equality implementation
    /// </summary>
    /// <param name="other">
    /// The other <code>EvmAddress</code> object to compare.
    /// </param>
    /// <returns>
    /// True if NFT, owner, created and metadata are the same.
    /// </returns>
    public bool Equals(EvmAddress? other)
    {
        return ReferenceEquals(this, other) || (other is not null && _bytes.AsSpan().SequenceEqual(other._bytes));
    }
    /// <summary>
    /// Equality implementation.
    /// </summary>
    /// <returns>
    /// A unique hash of the contents of this <code>EvmAddress</code> 
    /// object.  Only consistent within the current instance of 
    /// the application process.
    /// </returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            typeof(EvmAddress),
            BitConverter.ToInt64(_bytes, 0),
            BitConverter.ToInt64(_bytes, 8),
            BitConverter.ToInt32(_bytes, 16)
        );
    }
    /// <summary>
    /// Outputs an EIP-55 Checksum Encoding of EvmAddress 
    /// </summary>
    /// <returns>
    /// String representation of this EVM Address
    /// </returns>
    public override string ToString()
    {
        Span<char> hexChars = stackalloc char[40];
        Span<byte> utf8HexBytes = stackalloc byte[40];
        Hex.TryEncode(_bytes, hexChars, out _);
        Encoding.ASCII.GetBytes(hexChars, utf8HexBytes);
        var keccak = new KeccakDigest(256);
        keccak.BlockUpdate(utf8HexBytes.ToArray(), 0, 10);
        byte[] hash = new byte[32];
        keccak.DoFinal(hash, 0);
        var checksum = new StringBuilder("0x", 42);
        for (int i = 0; i < 40; i++)
        {
            byte nibble = (byte)((i % 2 == 0) ? hash[i / 2] >> 4 : hash[i / 2] & 0xF);
            char c = hexChars[i];
            checksum.Append(nibble >= 8 ? char.ToUpperInvariant(c) : c);
        }
        return checksum.ToString();
    }
    /// <summary>
    /// Tries to parse a string value into an EVM Payer address.
    /// </summary>
    /// <param name="value">The string representation of the EVM address, may start with '0x'</param>
    /// <param name="evmAddress">Contains the EVM Address if parsing is successful</param>
    /// <returns>True if parsing was successful, false if not.</returns>
    public static bool TryParse(string? value, [NotNullWhen(true)] out EvmAddress? evmAddress)
    {
        if (value != null && TryParse(value.AsSpan(), out evmAddress))
        {
            return true;
        }
        evmAddress = null;
        return false;
    }
    /// <summary>
    /// Tries to parse a string value into an EVM Payer address.
    /// </summary>
    /// <param name="value">The string representation of the EVM address, may start with '0x'</param>
    /// <param name="evmAddress">Contains the EVM Address if parsing is successful</param>
    /// <returns>True if parsing was successful, false if not.</returns>
    public static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out EvmAddress? evmAddress)
    {
        evmAddress = null;
        if (value.Length == 42 && value[0] == '0' && value[1] == 'x')
        {
            value = value[2..];
        }
        else if (value.Length != 40)
        {
            return false;
        }
        Span<byte> buffer = stackalloc byte[20];
        if (Hex.TryDecode(value, buffer, out int bytesWritten) && bytesWritten == 20)
        {
            evmAddress = new EvmAddress(buffer);
            return true;
        }
        return false;
    }
    /// <summary>
    /// Implicit operator for converting a readonly memory array 
    /// into an EVM Payer. It sets the shard and realm values to zero.
    /// </summary>
    /// <param name="bytes">
    /// The bytes representing the EVM Payer.
    /// </param>
    public static implicit operator EvmAddress(ReadOnlyMemory<byte> bytes)
    {
        return new EvmAddress(bytes.Span);
    }
    /// <summary>
    /// Implicit operator for converting a readonly span of bytes into 
    /// an EVM Payer.  It sets the shard and realm values to zero.
    /// </summary>
    /// <param name="bytes">
    /// The bytes representing the EVM Payer.
    /// </param>
    public static implicit operator EvmAddress(ReadOnlySpan<byte> bytes)
    {
        return new EvmAddress(bytes);
    }
    /// <summary>
    /// Implicitly wraps an EVM Payer within an
    /// <code>EntityId</code> construct.
    /// </summary>
    /// <param name="evmAddress">
    /// The EVM Payer to wrap within an <code>EntityId</code>.
    /// </param>
    public static implicit operator EntityId(EvmAddress evmAddress)
    {
        return new EntityId(0, 0, evmAddress);
    }
}
internal static class EvmAddressExtensions
{
    internal static bool IsNullOrNone([NotNullWhen(false)] this EvmAddress? moniker)
    {
        return moniker is null || moniker == EvmAddress.None;
    }
}