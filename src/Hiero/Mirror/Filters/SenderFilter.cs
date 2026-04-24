// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>sender.id</c> query parameter.
/// Construct via one of the static factories — the ctor is
/// private so the operator is always explicit at the call site.
/// </summary>
/// <remarks>
/// <para>
/// Mirror REST's <c>EntityIdQuery</c> schema accepts the six
/// comparison forms — equality (default), <c>gt:</c>,
/// <c>gte:</c>, <c>lt:</c>, <c>lte:</c>, and <c>ne:</c> — on
/// the <c>sender.id</c> query parameter. Each factory builds
/// the corresponding wire value.
/// </para>
/// <para>
/// Used by the pending-airdrops endpoint
/// (<c>/api/v1/accounts/{id}/airdrops/pending</c>) to narrow
/// results to airdrops originating from a particular sender.
/// Distinct from <see cref="EvmSenderFilter"/>, which targets
/// the EVM-level <c>from</c> address on contract-call endpoints.
/// </para>
/// </remarks>
public sealed class SenderFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "sender.id";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private SenderFilter(string value) => Value = value;

    /// <summary>
    /// Records whose <c>sender.id</c> equals the given account.
    /// </summary>
    /// <param name="sender">The sender account to filter by.</param>
    public static SenderFilter Is(EntityId sender) => new(sender.ToString());
    /// <summary>
    /// Records whose <c>sender.id</c> is strictly greater than
    /// the given account (<c>gt:</c>).
    /// </summary>
    /// <param name="sender">The sender account to filter by.</param>
    public static SenderFilter After(EntityId sender) => new($"gt:{sender}");
    /// <summary>
    /// Records whose <c>sender.id</c> is at or greater than the
    /// given account (<c>gte:</c>).
    /// </summary>
    /// <param name="sender">The sender account to filter by.</param>
    public static SenderFilter OnOrAfter(EntityId sender) => new($"gte:{sender}");
    /// <summary>
    /// Records whose <c>sender.id</c> is strictly less than the
    /// given account (<c>lt:</c>).
    /// </summary>
    /// <param name="sender">The sender account to filter by.</param>
    public static SenderFilter Before(EntityId sender) => new($"lt:{sender}");
    /// <summary>
    /// Records whose <c>sender.id</c> is at or less than the
    /// given account (<c>lte:</c>).
    /// </summary>
    /// <param name="sender">The sender account to filter by.</param>
    public static SenderFilter OnOrBefore(EntityId sender) => new($"lte:{sender}");
    /// <summary>
    /// Records whose <c>sender.id</c> is not equal to the given
    /// account (<c>ne:</c>).
    /// </summary>
    /// <param name="sender">The sender account to filter by.</param>
    public static SenderFilter NotIs(EntityId sender) => new($"ne:{sender}");
}
