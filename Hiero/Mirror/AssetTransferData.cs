using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;

/// <summary>
/// Represents an Nft (NFT) transfer within a transaction.
/// </summary>
public class AssetTransferData
{
    /// <summary>
    /// The identifier of the NFT token type transferred.
    /// </summary>
    [JsonPropertyName("token_id")]
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// The serial number of the Nft (NFT) transferred.
    /// </summary>
    [JsonPropertyName("serial_number")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long SerialNumber { get; set; }
    /// <summary>
    /// The account sending the asset.
    /// </summary>
    [JsonPropertyName("sender_account_id")]
    public EntityId Sender { get; set; } = default!;
    /// <summary>
    /// The account receiving the asset.
    /// </summary>
    [JsonPropertyName("receiver_account_id")]
    public EntityId Receiver { get; set; } = default!;
    /// <summary>
    /// Flag indiciating this transfer was performed
    /// as an allowed transfer by a third party account.
    /// </summary>
    [JsonPropertyName("is_approval")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool IsAllowance { get; set; }
}
