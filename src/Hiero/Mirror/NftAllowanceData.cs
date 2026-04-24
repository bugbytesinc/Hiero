// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using Hiero.Mirror.Paging;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// A single NFT-allowance record — a grant by an owner
/// authorizing a spender to transfer NFTs of a particular
/// token class on the owner's behalf. Returned by the
/// <c>/api/v1/accounts/{id}/allowances/nfts</c> mirror-node
/// endpoint.
/// </summary>
/// <remarks>
/// NFT allowances are always expressed per-token-class —
/// <see cref="ApprovedForAll"/> indicates whether the spender
/// can transfer every serial the owner currently or will later
/// hold, or just the specific serials granted elsewhere.
/// </remarks>
public class NftAllowanceData
{
    /// <summary>
    /// When <c>true</c>, the spender is authorized to transfer
    /// every NFT serial of this token class owned by the
    /// <see cref="Owner"/>, including serials acquired later.
    /// When <c>false</c>, the grant covers only specific serials
    /// (which are tracked elsewhere, not in this record).
    /// </summary>
    [JsonPropertyName("approved_for_all")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool ApprovedForAll { get; set; }
    /// <summary>
    /// The account granting the allowance.
    /// </summary>
    [JsonPropertyName("owner")]
    public EntityId Owner { get; set; } = default!;
    /// <summary>
    /// The account authorized to transfer NFTs of this token
    /// class on the owner's behalf.
    /// </summary>
    [JsonPropertyName("spender")]
    public EntityId Spender { get; set; } = default!;
    /// <summary>
    /// The consensus-timestamp range during which this
    /// allowance record is valid.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public TimestampRangeData? Timestamp { get; set; }
    /// <summary>
    /// The NFT token class this allowance applies to.
    /// </summary>
    [JsonPropertyName("token_id")]
    public EntityId Token { get; set; } = default!;
}
/// <summary>
/// Extension methods for querying NFT allowance data from the
/// mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class NftAllowanceDataExtensions
{
    /// <summary>
    /// Enumerates NFT allowances <b>granted by</b> this account —
    /// i.e., allowances where the given account is the owner and
    /// other accounts are authorized to transfer NFTs on its
    /// behalf. Maps to the endpoint's <c>owner=true</c> mode
    /// (the default server-side, but named explicitly here so the
    /// call site is self-describing).
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The owner account whose outgoing NFT allowances are
    /// requested.
    /// </param>
    /// <param name="filters">
    /// Additional query filters. The endpoint supports
    /// <see cref="AccountFilter"/> (for a specific spender),
    /// <see cref="TokenFilter"/>, <see cref="PageLimit"/>, and
    /// <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of NFT-allowance records.
    /// </returns>
    /// <seealso cref="GetAccountNftAllowancesAsSpenderAsync"/>
    public static IAsyncEnumerable<NftAllowanceData> GetAccountNftAllowancesAsOwnerAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{MirrorFormat(account)}/allowances/nfts", [new PageLimit(100), OwnerFlag.AsOwner, .. filters]);
        return client.GetPagedItemsAsync<NftAllowanceDataPage, NftAllowanceData>(path, MirrorJsonContext.Default.NftAllowanceDataPage);
    }
    /// <summary>
    /// Enumerates NFT allowances <b>granted to</b> this account —
    /// i.e., allowances where the given account is the spender
    /// and various owners have authorized it to transfer NFTs on
    /// their behalf. Maps to the endpoint's <c>owner=false</c>
    /// mode.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The spender account whose incoming NFT allowances are
    /// requested.
    /// </param>
    /// <param name="filters">
    /// Additional query filters. The endpoint supports
    /// <see cref="AccountFilter"/> (for a specific owner),
    /// <see cref="TokenFilter"/>, <see cref="PageLimit"/>, and
    /// <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of NFT-allowance records.
    /// </returns>
    /// <seealso cref="GetAccountNftAllowancesAsOwnerAsync"/>
    public static IAsyncEnumerable<NftAllowanceData> GetAccountNftAllowancesAsSpenderAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{MirrorFormat(account)}/allowances/nfts", [new PageLimit(100), OwnerFlag.AsSpender, .. filters]);
        return client.GetPagedItemsAsync<NftAllowanceDataPage, NftAllowanceData>(path, MirrorJsonContext.Default.NftAllowanceDataPage);
    }
}

// Internal owner-flag parameter used by the two NFT-allowance
// extension methods to pin the endpoint's `owner` query param.
// Kept file-scoped so it isn't discoverable in IntelliSense —
// the public API expresses the same choice via method selection.
file sealed class OwnerFlag : IMirrorQueryParameter
{
    public static readonly OwnerFlag AsOwner = new("true");
    public static readonly OwnerFlag AsSpender = new("false");
    public string Name => "owner";
    public string Value { get; }
    private OwnerFlag(string value) => Value = value;
}
