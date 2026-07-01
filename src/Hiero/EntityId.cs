// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Implementation.Parsing;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Hiero;
/// <summary>
/// Identifies a Hedera Address, Token, File, Topic, or Contract, typically
/// in the native format of <code>shard.realm.num</code>, but can also hold
/// a Key Alias or EVM Address if such an Entity Id type is required.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
[JsonConverter(typeof(EntityIdConverter))]
public sealed record EntityId : IEquatable<EntityId>, ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>
    /// Internal field to hold an EVM Address if this entity id
    /// was originally created as an EVM Address, or null if
    /// it was not.
    /// </summary>
    private readonly EvmAddress? _evmAddress;
    /// <summary>
    /// Internal field to hold a Key Alias if this entity id
    /// was originally created from a Key Alias, or null if
    /// it was not.
    /// </summary>
    private readonly Endorsement? _keyAlias;
    /// <summary>
    /// Network Shard Number for this Entity
    /// </summary>
    public long ShardNum { get; private init; }
    /// <summary>
    /// Network Realm Number for this Entity
    /// </summary>
    public long RealmNum { get; private init; }
    /// <summary>
    /// Network Address Number for this Entity,
    /// </summary>
    public long AccountNum { get; private init; }
    /// <summary>
    /// Indicates if this Entity Id contains a [shard.realm.num] native address.
    /// </summary>
    public bool IsShardRealmNum => _evmAddress is null && _keyAlias is null;
    /// <summary>
    /// Indicates if this Entity Id contains an EVM Address (EIP-1014) format.
    /// </summary>
    public bool IsEvmAddress => _evmAddress is not null;
    /// <summary>
    /// Indicates if this Entity Id contains a Key Alias format.
    /// </summary>
    public bool IsKeyAlias => _keyAlias is not null;
    /// <summary>
    /// A special designation of an Entity Id that can't be created.
    /// It represents the absence of a valid Entity Id.  The network will
    /// interpret as "no account/file/topic/token/contract" when applied 
    /// to change parameters. (typically the value null is interpreted 
    /// as "make no change"). In this way, it is possible to remove a 
    /// auto-renew account from a topic for example.
    /// </summary>
    public static EntityId None { get; } = new EntityId(0, 0, 0);
    /// <summary>
    /// Public Constructor, an <code>EntityId</code> is immutable after creation.
    /// </summary>
    /// <param name="shardNum">
    /// Network Shard Number
    /// </param>
    /// <param name="realmNum">
    /// Network Realm Number
    /// </param>
    /// <param name="accountNum">
    /// Network Address Number
    /// </param>
    public EntityId(long shardNum, long realmNum, long accountNum)
    {
        if (shardNum < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shardNum), "Shard Number cannot be negative.");
        }
        if (realmNum < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(realmNum), "Realm Number cannot be negative.");
        }
        if (accountNum < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(accountNum), "Account Number cannot be negative.");
        }
        ShardNum = shardNum;
        RealmNum = realmNum;
        AccountNum = accountNum;
    }

    /// <summary>
    /// Constructor creating a form of Entity Id that represents a Key Alias.
    /// </summary>
    /// <param name="shardNum">
    /// The shard number for this Entity ID.
    /// </param>
    /// <param name="realmNum">
    /// The realm number for this Entity ID.
    /// </param>
    /// <param name="keyAlias">
    /// The Ed25519 or ECDSA Secp 256K1 Endorsement this Entity Id will encapsulate.
    /// </param>
    public EntityId(long shardNum, long realmNum, Endorsement keyAlias)
    {
        if (shardNum < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shardNum), "Shard Number cannot be negative.");
        }
        if (realmNum < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(realmNum), "Realm Number cannot be negative.");
        }
        if (keyAlias.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(keyAlias), "Key Alias cannot be null or empty.");
        }
        if (keyAlias.Type != KeyType.Ed25519 && keyAlias.Type != KeyType.ECDSASecp256K1)
        {
            throw new ArgumentOutOfRangeException(nameof(keyAlias), "Unsupported key type, Endorsement must be a simple Ed25519 or ECDSA Secp 256K1 key type.");
        }
        ShardNum = shardNum;
        RealmNum = realmNum;
        AccountNum = 0;
        _keyAlias = keyAlias;
    }
    /// <summary>
    /// Constructor creating a form of Entity Id that represents an EVM Address.
    /// </summary>
    /// <param name="shardNum">
    /// The shard number for this Entity ID.
    /// </param>
    /// <param name="realmNum">
    /// The realm number for this Entity ID.
    /// </param>
    /// <param name="evmAddress">
    /// The EVM Address this Entity Id will encapsulate.
    /// </param>
    public EntityId(long shardNum, long realmNum, EvmAddress evmAddress)
    {
        if (shardNum < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shardNum), "Shard Number cannot be negative.");
        }
        if (realmNum < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(realmNum), "Realm Number cannot be negative.");
        }
        ShardNum = shardNum;
        RealmNum = realmNum;
        AccountNum = 0;
        _evmAddress = evmAddress;
    }
    /// <summary>
    /// Attempts to retrieve the EVM Address wrapped by this
    /// Entity Id instance.  Will return false if this Entity Id
    /// does not hold an EVM Address.
    /// </summary>
    /// <param name="evmAddress">
    /// Variable receiving the EVM Address instance if the
    /// operation is successful, otherwise <code>null</code>.
    /// </param>
    /// <returns>
    /// <code>True</code> if the entityId holds an EVM Address,
    /// otherwise false.
    /// </returns>
    public bool TryGetEvmAddress([MaybeNullWhen(false)] out EvmAddress evmAddress)
    {
        return (evmAddress = _evmAddress) is not null;
    }
    /// <summary>
    /// Attempts to retrieve the Key Alias Endorsement wrapped by 
    /// this Entity Id instance.  Will return false if this Entity Id
    /// does not hold a Key Alias.
    /// </summary>
    /// <param name="keyAlias">
    /// Variable receiving the Key Alias instance if the
    /// operation is successful, otherwise <code>null</code>.
    /// </param>
    /// <returns>
    /// <code>True</code> if the entityId holds a Key Alias,
    /// otherwise false.
    /// </returns>
    public bool TryGetKeyAlias([MaybeNullWhen(false)] out Endorsement keyAlias)
    {
        return (keyAlias = _keyAlias) is not null;
    }
    /// <summary>
    /// Outputs a string representation of the Entity Id
    /// (<code>shard.realm.num</code>), Key Alias
    /// or EVM Address.
    /// </summary>
    /// <returns>
    /// String representation of this account identifier in its
    /// entityId, keyAlias or evmAddress format.
    /// </returns>
    public override string ToString()
    {
        return $"{this}";
    }
    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();
    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_evmAddress is not null)
        {
            return _evmAddress.TryFormat(destination, out charsWritten, format, provider);
        }
        if (_keyAlias is not null)
        {
            return _keyAlias.TryFormat(destination, out charsWritten, format, provider);
        }
        charsWritten = 0;
        var required = ConverterExtensions.CountDigits(ShardNum) + ConverterExtensions.CountDigits(RealmNum) + ConverterExtensions.CountDigits(AccountNum) + 2;
        if (destination.Length < required)
        {
            return false;
        }
        if (!ShardNum.TryFormat(destination, out int w, default, provider)) return false;
        charsWritten += w;
        destination = destination[w..];
        if (destination.IsEmpty) return false;
        destination[0] = '.';
        destination = destination[1..];
        charsWritten++;
        if (!RealmNum.TryFormat(destination, out w, default, provider)) return false;
        charsWritten += w;
        destination = destination[w..];
        if (destination.IsEmpty) return false;
        destination[0] = '.';
        destination = destination[1..];
        charsWritten++;
        if (!AccountNum.TryFormat(destination, out w, default, provider)) return false;
        charsWritten += w;
        return true;
    }
    /// <inheritdoc />
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_evmAddress is not null)
        {
            return _evmAddress.TryFormat(utf8Destination, out bytesWritten, format, provider);
        }
        if (_keyAlias is not null)
        {
            return _keyAlias.TryFormat(utf8Destination, out bytesWritten, format, provider);
        }
        bytesWritten = 0;
        var required = ConverterExtensions.CountDigits(ShardNum) + ConverterExtensions.CountDigits(RealmNum) + ConverterExtensions.CountDigits(AccountNum) + 2;
        if (utf8Destination.Length < required)
        {
            return false;
        }
        if (!ShardNum.TryFormat(utf8Destination, out int w, default, provider)) return false;
        bytesWritten += w;
        utf8Destination = utf8Destination[w..];
        if (utf8Destination.IsEmpty) return false;
        utf8Destination[0] = (byte)'.';
        utf8Destination = utf8Destination[1..];
        bytesWritten++;
        if (!RealmNum.TryFormat(utf8Destination, out w, default, provider)) return false;
        bytesWritten += w;
        utf8Destination = utf8Destination[w..];
        if (utf8Destination.IsEmpty) return false;
        utf8Destination[0] = (byte)'.';
        utf8Destination = utf8Destination[1..];
        bytesWritten++;
        if (!AccountNum.TryFormat(utf8Destination, out w, default, provider)) return false;
        bytesWritten += w;
        return true;
    }
    /// <summary>
    /// Casts this Entity ID into the EVM Address format.
    /// If this ID already represents an underlying EVM Address,
    /// that instance value will be returned, otherwise the
    /// equivalent long-zero format of an EVM Address will
    /// be computed from shard.realm.num values, or an EOA 20-byte
    /// address will be computed from the Key Alias if it
    /// represents an ECDSA public key.
    /// </summary>
    /// <returns>
    /// An EVM Address compatible for use with smart contracts.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// If this Entity Id represents a Key Alias form of 
    /// an Entity ID, this will throw an exception, if the
    /// underlying key is not a single ECDSA public key.
    /// </exception>
    public EvmAddress CastToEvmAddress()
    {
        if (_evmAddress is not null)
        {
            return _evmAddress;
        }
        if (_keyAlias is not null)
        {
            // Note, will throw an exception if not proper type of
            // endorsement (which we would have to do anyway if
            // it was not done by the EvmAddress constructor)
            return new EvmAddress(_keyAlias);
        }
        // For 20 bytes total (aka uint160)
        // byte 0 to 3 are shard
        // byte 4 to 11 are realm
        // byte 12 to 19 are account number
        Span<byte> bytes = stackalloc byte[20];
        BinaryPrimitives.WriteUInt32BigEndian(bytes.Slice(0, 4), (uint)ShardNum);
        BinaryPrimitives.WriteUInt64BigEndian(bytes.Slice(4, 8), (ulong)RealmNum);
        BinaryPrimitives.WriteUInt64BigEndian(bytes.Slice(12, 8), (ulong)AccountNum);
        return new EvmAddress(bytes);
    }
    /// <summary>
    /// Attempts to parse a string as in the [shard.realm.num] format to convert
    /// into a Hedera Entity Id.
    /// </summary>
    /// <param name="value">
    /// The string form ([shard.realm.num]) of an Entity ID.
    /// </param>
    /// <param name="entityId">
    /// The resulting Entity Id if convertible, otherwise null.
    /// </param>
    /// <returns>
    /// True if the Entity Id could be converted and contains a valid value.
    /// </returns>
    public static bool TryParseShardRealmNum(string? value, [NotNullWhen(true)] out EntityId? entityId)
    {
        if (value != null && ShardRealmNumParser.TryParse(value.AsSpan(), out entityId))
        {
            return true;
        }
        entityId = null;
        return false;
    }
    /// <summary>
    /// Attempts to parse a span of characters (string) from in the [shard.realm.num] 
    /// format to convert into a Hedera Entity Id.
    /// </summary>
    /// <param name="value">
    /// The sequence of characters in the form ([shard.realm.num]) of an Entity ID.
    /// </param>
    /// <param name="entityId">
    /// The resulting Entity Id if convertible, otherwise null.
    /// </param>
    /// <returns>
    /// True if the Entity Id could be converted and contains a valid value.
    /// </returns>
    public static bool TryParseShardRealmNum(ReadOnlySpan<char> value, [NotNullWhen(true)] out EntityId? entityId)
    {
        return ShardRealmNumParser.TryParse(value, out entityId);
    }
    /// <summary>
    /// Determines if this Entity Id is equal to another Entity Id.
    /// </summary>
    /// <param name="other">
    /// The other <code>EntityId</code> object to compare.
    /// </param>
    /// <returns>
    /// True if these represent the same entity, otherwise false.
    /// </returns>
    public bool Equals(EntityId? other)
    {
        if (other is null)
        {
            return false;
        }
        return ReferenceEquals(this, other) || (
            ShardNum == other.ShardNum &&
            RealmNum == other.RealmNum &&
            AccountNum == other.AccountNum &&
            (_evmAddress?.Equals(other._evmAddress) ?? other._evmAddress is null) &&
            (_keyAlias?.Equals(other._keyAlias) ?? other._keyAlias is null));
    }
    /// <summary>
    /// Equality implementation.
    /// </summary>
    /// <returns>
    /// A unique hash of the contents of this <code>EntityId</code> 
    /// object.  Only consistent within the current instance of 
    /// the application process.
    /// </returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(typeof(EntityId));
        hash.Add(ShardNum);
        hash.Add(RealmNum);
        hash.Add(AccountNum);
        if (_evmAddress is not null)
        {
            hash.Add(_evmAddress);
        }
        if (_keyAlias is not null)
        {
            hash.Add(_keyAlias);
        }
        return hash.ToHashCode();
    }
    internal string ToMirrorString()
    {
        if (_evmAddress is not null)
        {
            return Convert.ToHexStringLower(_evmAddress.Bytes);
        }
        if (_keyAlias is not null)
        {
            return Convert.ToHexStringLower(_keyAlias.ToBytes(KeyFormat.Mirror).Span);
        }
        return $"{ShardNum}.{RealmNum}.{AccountNum}";
    }
}
internal static class EntityIdExtensions
{
    internal static bool IsNullOrNone([NotNullWhen(false)] this EntityId? address)
    {
        return address is null || address == EntityId.None;
    }
}
