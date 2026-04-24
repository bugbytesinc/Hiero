// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>transaction.hash</c> query parameter of
/// the cross-contract logs endpoint (<c>/api/v1/contracts/results/logs</c>).
/// Scopes the returned log events to a single transaction without
/// needing to know which contract emitted them.
/// </summary>
/// <remarks>
/// The wire format accepts **either** a 32-byte Ethereum transaction
/// hash (64 hex chars) **or** a 48-byte Hedera SHA-384 transaction hash
/// (96 hex chars). Use <see cref="Is(EvmHash)"/> when you already have
/// an <see cref="EvmHash"/>; use <see cref="Is(ReadOnlyMemory{byte})"/>
/// for raw bytes (EVM 32-byte or Hedera 48-byte). Construct via the
/// static factories; the ctor is private.
/// </remarks>
public sealed class TransactionHashFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "transaction.hash";
    /// <summary>
    /// The value of the query parameter sent to the mirror node — a
    /// <c>0x</c>-prefixed lowercase hex string, 64 or 96 characters.
    /// </summary>
    public string Value { get; }

    private TransactionHashFilter(string value) => Value = value;

    /// <summary>
    /// Records whose <c>transaction.hash</c> equals the given 32-byte
    /// EVM transaction hash.
    /// </summary>
    /// <param name="evmHash">The EVM transaction hash (32 bytes, keccak-256).</param>
    public static TransactionHashFilter Is(EvmHash evmHash) =>
        new(evmHash.ToString());

    /// <summary>
    /// Records whose <c>transaction.hash</c> equals the given raw hash
    /// bytes. Accepts 32-byte EVM hashes and 48-byte Hedera SHA-384
    /// hashes; any other length throws.
    /// </summary>
    /// <param name="hash">The transaction hash bytes — must be 32 or 48 bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="hash"/> is neither 32 nor 48 bytes long.
    /// </exception>
    public static TransactionHashFilter Is(ReadOnlyMemory<byte> hash)
    {
        if (hash.Length != 32 && hash.Length != 48)
        {
            throw new ArgumentOutOfRangeException(nameof(hash), "Transaction hash must be 32 bytes (EVM) or 48 bytes (Hedera SHA-384).");
        }
        return new($"0x{Hex.FromBytes(hash.Span)}");
    }
}
