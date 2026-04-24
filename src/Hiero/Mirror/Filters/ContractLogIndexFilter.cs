// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>index</c> query parameter of the two
/// contract-log list endpoints —
/// <c>/api/v1/contracts/results/logs</c> and
/// <c>/api/v1/contracts/{contractIdOrAddress}/results/logs</c>.
/// Narrows results to contract-log entries at a specific block-local
/// position (or to a comparison range thereof). Construct via one of
/// the static factories — the ctor is private so the operator is
/// always explicit at the call site.
/// </summary>
/// <remarks>
/// <para>
/// Unlike the sibling <see cref="ContractActionIndexFilter"/> — which
/// shares the same <c>index</c> wire name on the contract-actions
/// endpoint — the log endpoints accept only five operators on the
/// wire: equality (default), <c>gt:</c>, <c>gte:</c>, <c>lt:</c>, and
/// <c>lte:</c>. The schema regex
/// <c>^((eq|gt|gte|lt|lte):)?\d{1,10}$</c> explicitly excludes
/// <c>ne:</c>, so no <c>NotIs</c> factory is exposed.
/// </para>
/// <para>
/// The mirror-node spec also requires a <see cref="TimestampFilter"/>
/// to be present in the same request whenever this filter appears —
/// the server 400s otherwise. The SDK does not enforce this client
/// side; pass both filters together.
/// </para>
/// <para>
/// This filter is deliberately distinct from
/// <see cref="ContractActionIndexFilter"/>: the two endpoints share
/// the <c>index</c> wire name but differ in semantic scope
/// (action-index vs. log-index) and in operator palette
/// (actions accept <c>ne</c>, logs do not).
/// </para>
/// </remarks>
public sealed class ContractLogIndexFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "index";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private ContractLogIndexFilter(string value) => Value = value;

    /// <summary>
    /// Log entries whose index equals the given value.
    /// </summary>
    public static ContractLogIndexFilter Is(int index) => Build(index, null);
    /// <summary>
    /// Log entries whose index is strictly greater than the given
    /// value (<c>gt:</c>).
    /// </summary>
    public static ContractLogIndexFilter After(int index) => Build(index, "gt");
    /// <summary>
    /// Log entries whose index is at or greater than the given value
    /// (<c>gte:</c>).
    /// </summary>
    public static ContractLogIndexFilter OnOrAfter(int index) => Build(index, "gte");
    /// <summary>
    /// Log entries whose index is strictly less than the given value
    /// (<c>lt:</c>).
    /// </summary>
    public static ContractLogIndexFilter Before(int index) => Build(index, "lt");
    /// <summary>
    /// Log entries whose index is at or less than the given value
    /// (<c>lte:</c>).
    /// </summary>
    public static ContractLogIndexFilter OnOrBefore(int index) => Build(index, "lte");

    private static ContractLogIndexFilter Build(int index, string? op)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Log index must be non-negative.");
        }
        return op is null
            ? new ContractLogIndexFilter(index.ToString())
            : new ContractLogIndexFilter($"{op}:{index}");
    }
}
