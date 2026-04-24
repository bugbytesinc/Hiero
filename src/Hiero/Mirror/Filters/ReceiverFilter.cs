// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>receiver.id</c> query parameter.
/// Construct via one of the static factories — the ctor is
/// private so the operator is always explicit at the call site.
/// </summary>
/// <remarks>
/// <para>
/// Mirror REST's <c>EntityIdQuery</c> schema accepts the six
/// comparison forms — equality (default), <c>gt:</c>,
/// <c>gte:</c>, <c>lt:</c>, <c>lte:</c>, and <c>ne:</c> — on
/// the <c>receiver.id</c> query parameter. Each factory builds
/// the corresponding wire value.
/// </para>
/// <para>
/// Used by the outstanding-airdrops endpoint
/// (<c>/api/v1/accounts/{id}/airdrops/outstanding</c>) to narrow
/// results to airdrops destined for a particular receiver.
/// </para>
/// </remarks>
public sealed class ReceiverFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "receiver.id";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private ReceiverFilter(string value) => Value = value;

    /// <summary>
    /// Records whose <c>receiver.id</c> equals the given account.
    /// </summary>
    /// <param name="receiver">The receiver account to filter by.</param>
    public static ReceiverFilter Is(EntityId receiver) => new(receiver.ToString());
    /// <summary>
    /// Records whose <c>receiver.id</c> is strictly greater than
    /// the given account (<c>gt:</c>).
    /// </summary>
    /// <param name="receiver">The receiver account to filter by.</param>
    public static ReceiverFilter After(EntityId receiver) => new($"gt:{receiver}");
    /// <summary>
    /// Records whose <c>receiver.id</c> is at or greater than the
    /// given account (<c>gte:</c>).
    /// </summary>
    /// <param name="receiver">The receiver account to filter by.</param>
    public static ReceiverFilter OnOrAfter(EntityId receiver) => new($"gte:{receiver}");
    /// <summary>
    /// Records whose <c>receiver.id</c> is strictly less than the
    /// given account (<c>lt:</c>).
    /// </summary>
    /// <param name="receiver">The receiver account to filter by.</param>
    public static ReceiverFilter Before(EntityId receiver) => new($"lt:{receiver}");
    /// <summary>
    /// Records whose <c>receiver.id</c> is at or less than the
    /// given account (<c>lte:</c>).
    /// </summary>
    /// <param name="receiver">The receiver account to filter by.</param>
    public static ReceiverFilter OnOrBefore(EntityId receiver) => new($"lte:{receiver}");
    /// <summary>
    /// Records whose <c>receiver.id</c> is not equal to the given
    /// account (<c>ne:</c>).
    /// </summary>
    /// <param name="receiver">The receiver account to filter by.</param>
    public static ReceiverFilter NotIs(EntityId receiver) => new($"ne:{receiver}");
}
