// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>account.publickey</c> query
/// parameter used by the account-list and balance-snapshot
/// endpoints. Matches accounts whose root public key equals
/// the given endorsement.
/// </summary>
/// <remarks>
/// <para>
/// OpenAPI defines this parameter as a plain <c>type: string</c>
/// with no operator prefix, so the filter exposes equality only
/// — there is no <c>gt:</c> / <c>lt:</c> operator set on the
/// wire side.
/// </para>
/// <para>
/// Distinct from <see cref="PublicKeyFilter"/> (which targets the
/// bare <c>publickey</c> wire param on <c>/api/v1/tokens</c>).
/// Same encoding shape — hex form of the endorsement's
/// mirror-format bytes — but a different wire name.
/// </para>
/// </remarks>
public sealed class AccountPublicKeyFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "account.publickey";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// raw hex form of the key's mirror-format bytes, without a
    /// <c>0x</c> prefix.
    /// </summary>
    public string Value { get; }

    private AccountPublicKeyFilter(string value) => Value = value;

    /// <summary>
    /// Records whose root public key equals the given
    /// endorsement. The endorsement's mirror-format bytes are
    /// hex-encoded for transport.
    /// </summary>
    /// <param name="endorsement">
    /// The endorsement carrying the public key to match against.
    /// </param>
    public static AccountPublicKeyFilter Is(Endorsement endorsement)
    {
        ArgumentNullException.ThrowIfNull(endorsement);
        return new AccountPublicKeyFilter(Hex.FromBytes(endorsement.ToBytes(KeyFormat.Mirror)));
    }
}
