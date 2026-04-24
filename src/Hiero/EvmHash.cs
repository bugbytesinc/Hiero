// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Hiero;
/// <summary>
/// Represents a 32-byte EVM hash, such as a keccak256 transaction
/// hash or block hash returned by the Mirror Node REST API.
/// </summary>
/// <remarks>
/// This is a value-type wrapper that distinguishes an EVM-style
/// 32-byte hash from arbitrary <c>ReadOnlyMemory&lt;byte&gt;</c> payloads
/// at the type level. Use it for transaction hashes, block hashes,
/// and log topic values where the wire representation is a
/// 32-byte keccak output. Hedera-native hashes (e.g., SHA-384
/// record-file hashes) are not 32 bytes and should not be represented
/// with this type.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
[JsonConverter(typeof(EvmHashConverter))]
public sealed record EvmHash : IEquatable<EvmHash>
{
    /// <summary>
    /// Internal storage for the 32-byte hash.
    /// </summary>
    private readonly byte[] _bytes = new byte[32];
    /// <summary>
    /// The 32 bytes of the hash.
    /// </summary>
    public ReadOnlySpan<byte> Bytes => _bytes;
    /// <summary>
    /// The zero hash (32 bytes of 0x00). Used as a sentinel for
    /// "no hash"; distinguishable from a valid hash because a
    /// real keccak256 output of all zeros is cryptographically
    /// improbable.
    /// </summary>
    public static EvmHash None { get; } = new EvmHash(new byte[32].AsSpan());
    /// <summary>
    /// Public Constructor, an <c>EvmHash</c> is immutable after
    /// construction.
    /// </summary>
    /// <param name="bytes">
    /// The 32 bytes of the hash.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// When the length of <paramref name="bytes"/> is not 32.
    /// </exception>
    public EvmHash(ReadOnlyMemory<byte> bytes) : this(bytes.Span)
    {
    }
    /// <summary>
    /// Public Constructor, an <c>EvmHash</c> is immutable after
    /// construction.
    /// </summary>
    /// <param name="bytes">
    /// The 32 bytes of the hash.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// When the length of <paramref name="bytes"/> is not 32.
    /// </exception>
    public EvmHash(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 32)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "The encoded bytes must have a length of 32.");
        }
        bytes.CopyTo(_bytes);
    }
    /// <summary>
    /// Equality implementation.
    /// </summary>
    /// <param name="other">
    /// The other <c>EvmHash</c> object to compare.
    /// </param>
    /// <returns>
    /// True if the hash bytes are identical.
    /// </returns>
    public bool Equals(EvmHash? other)
    {
        return ReferenceEquals(this, other) || (other is not null && _bytes.AsSpan().SequenceEqual(other._bytes));
    }
    /// <summary>
    /// Equality implementation.
    /// </summary>
    /// <returns>
    /// A unique hash of the contents of this <c>EvmHash</c>
    /// object.  Only consistent within the current instance of
    /// the application process.
    /// </returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            typeof(EvmHash),
            BitConverter.ToInt64(_bytes, 0),
            BitConverter.ToInt64(_bytes, 8),
            BitConverter.ToInt64(_bytes, 16),
            BitConverter.ToInt64(_bytes, 24)
        );
    }
    /// <summary>
    /// Outputs the hash as a <c>0x</c>-prefixed 64-character
    /// lowercase hex string.
    /// </summary>
    public override string ToString()
    {
        Span<char> hexChars = stackalloc char[64];
        Hex.TryEncode(_bytes, hexChars, out _);
        return string.Concat("0x", hexChars);
    }
    /// <summary>
    /// Tries to parse a string value into an <c>EvmHash</c>.
    /// </summary>
    /// <param name="value">
    /// The string representation of the hash. May optionally start
    /// with <c>0x</c>; must otherwise be exactly 64 hexadecimal
    /// characters.
    /// </param>
    /// <param name="hash">
    /// Contains the <c>EvmHash</c> if parsing is successful.
    /// </param>
    /// <returns>True if parsing was successful, false if not.</returns>
    public static bool TryParse(string? value, [NotNullWhen(true)] out EvmHash? hash)
    {
        if (value != null && TryParse(value.AsSpan(), out hash))
        {
            return true;
        }
        hash = null;
        return false;
    }
    /// <summary>
    /// Tries to parse a string value into an <c>EvmHash</c>.
    /// </summary>
    /// <param name="value">
    /// The string representation of the hash. May optionally start
    /// with <c>0x</c>; must otherwise be exactly 64 hexadecimal
    /// characters.
    /// </param>
    /// <param name="hash">
    /// Contains the <c>EvmHash</c> if parsing is successful.
    /// </param>
    /// <returns>True if parsing was successful, false if not.</returns>
    public static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out EvmHash? hash)
    {
        hash = null;
        if (value.Length == 66 && value[0] == '0' && value[1] == 'x')
        {
            value = value[2..];
        }
        else if (value.Length != 64)
        {
            return false;
        }
        Span<byte> buffer = stackalloc byte[32];
        if (Hex.TryDecode(value, buffer, out int bytesWritten) && bytesWritten == 32)
        {
            hash = new EvmHash(buffer);
            return true;
        }
        return false;
    }
    /// <summary>
    /// Implicit operator for converting a read-only memory of bytes
    /// into an <c>EvmHash</c>.
    /// </summary>
    /// <param name="bytes">
    /// The 32 bytes of the hash.
    /// </param>
    public static implicit operator EvmHash(ReadOnlyMemory<byte> bytes)
    {
        return new EvmHash(bytes.Span);
    }
    /// <summary>
    /// Implicit operator for converting a read-only span of bytes
    /// into an <c>EvmHash</c>.
    /// </summary>
    /// <param name="bytes">
    /// The 32 bytes of the hash.
    /// </param>
    public static implicit operator EvmHash(ReadOnlySpan<byte> bytes)
    {
        return new EvmHash(bytes);
    }
}
internal static class EvmHashExtensions
{
    internal static bool IsNullOrNone([NotNullWhen(false)] this EvmHash? hash)
    {
        return hash is null || hash == EvmHash.None;
    }
}
