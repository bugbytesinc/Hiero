// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>transaction.index</c> query
/// parameter — narrows contract-result listings to the given
/// in-block position. Construct via <see cref="Is(int)"/>; the
/// ctor is private.
/// </summary>
/// <remarks>
/// The OpenAPI schema for this parameter is a plain
/// <c>format: int32, minimum: 0</c> integer with no
/// operator-pattern definition. Conservatively exposes
/// equality only — if the mirror node accepts comparison
/// operators at runtime that's a trivial additive expansion of
/// this type later.
/// </remarks>
public sealed class TransactionIndexFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "transaction.index";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private TransactionIndexFilter(string value) => Value = value;

    /// <summary>
    /// Records whose in-block transaction index equals the given
    /// value.
    /// </summary>
    /// <param name="index">
    /// The zero-based in-block position; must not be negative.
    /// </param>
    public static TransactionIndexFilter Is(int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Transaction index must not be negative.");
        }
        return new TransactionIndexFilter(index.ToString());
    }
}
