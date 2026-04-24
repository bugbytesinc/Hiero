// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>index</c> position of a contract
/// action within a transaction's ordered call graph. Construct
/// via one of the static factories — the ctor is private so the
/// operator is always explicit in the call site.
/// </summary>
/// <remarks>
/// <para>
/// Mirror REST accepts the six comparison forms — equality
/// (default), <c>gt:</c>, <c>gte:</c>, <c>lt:</c>, <c>lte:</c>, and
/// <c>ne:</c> — on the <c>index</c> query parameter of the
/// contract-actions endpoint (regex
/// <c>^((gte?|lte?|eq|ne)\:)?\d{1,10}$</c>). Each factory builds
/// the corresponding wire value.
/// </para>
/// <para>
/// The <c>index</c> wire-name is also accepted on contract-log
/// endpoints, where it refers to a log's block-local position — a
/// separate semantic scope with a different operator palette (the
/// log endpoints reject <c>ne:</c>). For the log-endpoint parallel,
/// see <see cref="ContractLogIndexFilter"/>.
/// </para>
/// </remarks>
public sealed class ContractActionIndexFilter : IMirrorFilter
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

    private ContractActionIndexFilter(string value) => Value = value;

    /// <summary>
    /// Actions whose index equals the given value.
    /// </summary>
    public static ContractActionIndexFilter Is(int index) => Build(index, null);
    /// <summary>
    /// Actions whose index is strictly greater than the given value
    /// (<c>gt:</c>).
    /// </summary>
    public static ContractActionIndexFilter After(int index) => Build(index, "gt");
    /// <summary>
    /// Actions whose index is at or greater than the given value
    /// (<c>gte:</c>).
    /// </summary>
    public static ContractActionIndexFilter OnOrAfter(int index) => Build(index, "gte");
    /// <summary>
    /// Actions whose index is strictly less than the given value
    /// (<c>lt:</c>).
    /// </summary>
    public static ContractActionIndexFilter Before(int index) => Build(index, "lt");
    /// <summary>
    /// Actions whose index is at or less than the given value
    /// (<c>lte:</c>).
    /// </summary>
    public static ContractActionIndexFilter OnOrBefore(int index) => Build(index, "lte");
    /// <summary>
    /// Actions whose index is not equal to the given value
    /// (<c>ne:</c>).
    /// </summary>
    public static ContractActionIndexFilter NotIs(int index) => Build(index, "ne");

    private static ContractActionIndexFilter Build(int index, string? op)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Action index must be non-negative.");
        }
        return op is null
            ? new ContractActionIndexFilter(index.ToString())
            : new ContractActionIndexFilter($"{op}:{index}");
    }
}
