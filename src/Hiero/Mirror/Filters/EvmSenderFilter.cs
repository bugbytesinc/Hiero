// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>from</c> query parameter of the
/// contract-results endpoints — the EVM sender (<c>msg.sender</c>)
/// of a contract call, as a 20-byte EVM address. Construct via
/// <see cref="Is(EvmAddress)"/>; the ctor is private.
/// </summary>
/// <remarks>
/// The wire parameter is named <c>from</c>, not <c>contract.id</c> —
/// this is the EVM-side sender address, not a Hedera contract
/// entity id. For filtering by <c>contract.id</c> on the
/// <c>/api/v1/contracts</c> list endpoint, use
/// <see cref="ContractFilter"/> instead.
/// </remarks>
public sealed class EvmSenderFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "from";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private EvmSenderFilter(string value) => Value = value;

    /// <summary>
    /// Records whose <c>from</c> (EVM sender) equals the given
    /// 20-byte address.
    /// </summary>
    /// <param name="sender">The EVM sender address.</param>
    public static EvmSenderFilter Is(EvmAddress sender) =>
        new($"0x{Hex.FromBytes(sender.Bytes)}");
}
