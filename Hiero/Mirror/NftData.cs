using Hiero.Converters;
using Hiero.Mirror.Filters;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Nft (NFT) information retrieved from a mirror node.
/// </summary>
public class NftData
{
    /// <summary>
    /// The Current Holder of the Nft (NFT)
    /// </summary>
    [JsonPropertyName("account_id")]
    public EntityId Owner { get; set; } = default!;
    /// <summary>
    /// The consensus timestamp of the asset's creation.
    /// </summary>
    [JsonPropertyName("created_timestamp")]
    public ConsensusTimeStamp Created { get; set; }
    /// <summary>
    /// An account that is permitted to creat allowances for this
    /// Nft on the owner's behalf.
    /// </summary>
    [JsonPropertyName("delegating_spender")]
    public EntityId DelegatingSpender { get; set; } = default!;
    /// <summary>
    /// Flag indicating the asset has been deleted.
    /// </summary>
    [JsonPropertyName("deleted")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool Deleted { get; set; }
    /// <summary>
    /// The associated Nft metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public string Metadata { get; set; } = default!;
    /// <summary>
    /// The last time this asset was modified.
    /// </summary>
    [JsonPropertyName("modified_timestamp")]
    public ConsensusTimeStamp Modified { get; set; }
    /// <summary>
    /// The serial number of the asset.
    /// </summary>
    [JsonPropertyName("serial_number")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long SerialNumber { get; set; }
    /// <summary>
    /// An account that is permitted to transfer this asset
    /// Nft on the owner's behalf.
    /// </summary>
    [JsonPropertyName("spender")]
    public EntityId Spender { get; set; } = default!;
    /// <summary>
    /// The Hedera token address of this asset class.
    /// </summary>
    [JsonPropertyName("token_id")]
    public EntityId Token { get; set; } = default!;
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class NftDataExtensions
{
    /// <summary>
    /// Retrieves information for the given asset.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request./// </param>
    /// <param name="nft">
    /// The identifier of the NFT to retrieve, which includes the token address 
    /// and the serial number of the NFT.
    /// </param>
    /// <param name="filters">
    /// Optional list of filter constraints for this query.
    /// </param>
    /// <returns>
    /// The asset information.
    /// </returns>
    public static Task<NftData?> GetNftAsync(this MirrorRestClient client, Nft nft, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"tokens/{nft.Token}/nfts/{nft.SerialNumber}", filters);
        return client.GetSingleItemAsync<NftData>(path);
    }
}