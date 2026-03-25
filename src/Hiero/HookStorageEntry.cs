// SPDX-License-Identifier: Apache-2.0
namespace Hiero;
/// <summary>
/// Represents a single storage entry for an EVM hook,
/// either a direct storage slot or an entry within a
/// Solidity mapping.
/// </summary>
/// <remarks>
/// When <see cref="IndexKey"/> is <c>null</c>, this represents
/// a direct storage slot update identified by <see cref="Key"/>.
/// When <see cref="IndexKey"/> is set, this represents an entry
/// in a Solidity mapping where <see cref="Key"/> is the mapping's
/// base slot and <see cref="IndexKey"/> is the key into the mapping.
/// </remarks>
public sealed record HookStorageEntry
{
    /// <summary>
    /// The storage slot key (for direct slots) or the mapping's
    /// base slot (for mapping entries). Minimal representation,
    /// max 32 bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Key { get; private init; }
    /// <summary>
    /// When set, indicates this is a mapping entry and contains
    /// the key into the Solidity mapping. When <c>null</c>, this
    /// is a direct storage slot update.
    /// </summary>
    public ReadOnlyMemory<byte>? IndexKey { get; private init; }
    /// <summary>
    /// When <see cref="IndexKey"/> is set, indicates whether it
    /// is the preimage of the Keccak256 hash rather than an
    /// explicit mapping key. Preimage keys may be variable length
    /// and can include leading zeros.
    /// </summary>
    public bool IsPreimage { get; private init; }
    /// <summary>
    /// The value for the storage slot or mapping entry.
    /// Minimal representation, max 32 bytes. An empty value
    /// removes the entry.
    /// </summary>
    public ReadOnlyMemory<byte> Value { get; private init; }
    /// <summary>
    /// Creates a direct storage slot entry.
    /// </summary>
    /// <param name="key">
    /// The key of the storage slot (minimal representation, max 32 bytes).
    /// </param>
    /// <param name="value">
    /// The value for the slot (minimal representation, max 32 bytes, empty removes it).
    /// </param>
    public HookStorageEntry(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> value)
    {
        Key = key;
        Value = value;
    }
    /// <summary>
    /// Creates a mapping entry within a Solidity mapping.
    /// </summary>
    /// <param name="key">
    /// The base slot of the Solidity mapping (minimal representation, max 32 bytes).
    /// </param>
    /// <param name="indexKey">
    /// The key into the mapping (explicit bytes or preimage, depending on
    /// <paramref name="isPreimage"/>).
    /// </param>
    /// <param name="value">
    /// The value for the mapping entry (max 32 bytes, empty removes it).
    /// </param>
    /// <param name="isPreimage">
    /// When <c>true</c>, <paramref name="indexKey"/> is the preimage of the
    /// Keccak256 hash that forms the mapping key.
    /// </param>
    public HookStorageEntry(ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> indexKey, ReadOnlyMemory<byte> value, bool isPreimage = false)
    {
        Key = key;
        IndexKey = indexKey;
        Value = value;
        IsPreimage = isPreimage;
    }
}
