// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>contract.id</c> query parameter of
/// the <c>/api/v1/contracts</c> list endpoint. Construct via one
/// of the static factories — the ctor is private so the
/// operator is always explicit at the call site.
/// </summary>
/// <remarks>
/// <para>
/// The mirror-node wire pattern for <c>contract.id</c> admits
/// the six comparison forms — equality (default), <c>gt:</c>,
/// <c>gte:</c>, <c>lt:</c>, <c>lte:</c>, and <c>ne:</c> —
/// matching the <c>EntityIdQuery</c> precedent. The pattern
/// additionally accepts an EVM-address form on the value side,
/// which <see cref="EntityId"/>'s <c>ToString</c> emits when the
/// instance carries one.
/// </para>
/// <para>
/// Distinct from <see cref="EvmSenderFilter"/>, which filters
/// the EVM-side <c>from</c> address on the contract-results
/// endpoints.
/// </para>
/// </remarks>
public sealed class ContractFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "contract.id";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private ContractFilter(string value) => Value = value;

    /// <summary>
    /// Records whose <c>contract.id</c> equals the given entity.
    /// </summary>
    /// <param name="contract">The contract entity to filter by.</param>
    public static ContractFilter Is(EntityId contract) => new(contract.ToString());
    /// <summary>
    /// Records whose <c>contract.id</c> is strictly greater than
    /// the given entity (<c>gt:</c>).
    /// </summary>
    /// <param name="contract">The contract entity to filter by.</param>
    public static ContractFilter After(EntityId contract) => new($"gt:{contract}");
    /// <summary>
    /// Records whose <c>contract.id</c> is at or greater than the
    /// given entity (<c>gte:</c>).
    /// </summary>
    /// <param name="contract">The contract entity to filter by.</param>
    public static ContractFilter OnOrAfter(EntityId contract) => new($"gte:{contract}");
    /// <summary>
    /// Records whose <c>contract.id</c> is strictly less than the
    /// given entity (<c>lt:</c>).
    /// </summary>
    /// <param name="contract">The contract entity to filter by.</param>
    public static ContractFilter Before(EntityId contract) => new($"lt:{contract}");
    /// <summary>
    /// Records whose <c>contract.id</c> is at or less than the
    /// given entity (<c>lte:</c>).
    /// </summary>
    /// <param name="contract">The contract entity to filter by.</param>
    public static ContractFilter OnOrBefore(EntityId contract) => new($"lte:{contract}");
    /// <summary>
    /// Records whose <c>contract.id</c> is not equal to the given
    /// entity (<c>ne:</c>).
    /// </summary>
    /// <param name="contract">The contract entity to filter by.</param>
    public static ContractFilter NotIs(EntityId contract) => new($"ne:{contract}");
}
