// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>publickey</c> query parameter of
/// the <c>/api/v1/tokens</c> list endpoint. Matches tokens
/// whose admin / supply / wipe / etc. key equals the given
/// public key.
/// </summary>
/// <remarks>
/// <para>
/// Matches all key slots exposed by <c>TokenInfo</c>: admin,
/// freeze, kyc, supply, wipe, fee-schedule, pause, metadata.
/// The mirror node determines which slot matches — the filter
/// itself doesn't narrow further.
/// </para>
/// <para>
/// Note: this targets the bare <c>publickey</c> query parameter
/// used by <c>/tokens</c>. The distinct <c>account.publickey</c>
/// parameter on <c>/accounts</c>, <c>/balances</c>, and
/// <c>/tokens/{id}/balances</c> is handled by a separate filter
/// type (not yet introduced).
/// </para>
/// </remarks>
public sealed class PublicKeyFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "publickey";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// raw hex form of the key's mirror-format bytes, without a
    /// <c>0x</c> prefix.
    /// </summary>
    public string Value { get; }

    private PublicKeyFilter(string value) => Value = value;

    /// <summary>
    /// Matches records whose public key equals the given
    /// endorsement. The endorsement's mirror-format bytes are
    /// hex-encoded for transport.
    /// </summary>
    /// <param name="endorsement">
    /// The endorsement carrying the public key to match against.
    /// </param>
    public static PublicKeyFilter Is(Endorsement endorsement)
    {
        ArgumentNullException.ThrowIfNull(endorsement);
        return new PublicKeyFilter(Hex.FromBytes(endorsement.ToBytes(KeyFormat.Mirror)));
    }
}
