// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;
/// <summary>
/// Represents a transaction retrieved from a mirror node.
/// </summary>
/// <remarks>
/// This is the base shape shared by the transaction list and
/// detail endpoints. The single-transaction lookups
/// (<see cref="TransactionDetailDataExtensions"/>) return the
/// richer <see cref="TransactionDetailData"/> subclass, which adds
/// assessed custom fees.
/// </remarks>
public class TransactionData
{
    /// <summary>
    /// The unique identifier for this transaction.
    /// </summary>
    [JsonPropertyName("transaction_id")]
    [JsonConverter(typeof(TransactionIdMirrorConverter))]
    public TransactionId TransactionId { get; set; } = default!;
    /// <summary>
    /// The endorsement that must sign the enclosing atomic batch for
    /// this transaction to execute. Null unless this is an inner
    /// transaction of a batch; the outer batch transaction itself
    /// carries none.
    /// </summary>
    [JsonPropertyName("batch_key")]
    public Endorsement? BatchEndorsement { get; set; }
    /// <summary>
    /// Sum of network transaction fees.
    /// </summary>
    [JsonPropertyName("charged_tx_fee")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Fee { get; set; }
    /// <summary>
    /// The transaction’s consensus timestamp.
    /// </summary>
    [JsonPropertyName("consensus_timestamp")]
    public ConsensusTimeStamp Consensus { get; set; }
    /// <summary>
    /// The address of any entity created by this
    /// transaction (such as the created account or contract)
    /// </summary>
    [JsonPropertyName("entity_id")]
    public EntityId? CreatedEntity { get; set; }
    /// <summary>
    /// Whether the transaction was throttled and priced as a
    /// high-volume entity creation (HIP-1313).
    /// </summary>
    [JsonPropertyName("high_volume")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool HighVolume { get; set; }
    /// <summary>
    /// The high-volume pricing multiplier applied to the transaction
    /// fee (HIP-1313), scaled by 1,000: 1,000 is 1×, 4,000 is 4×. Zero
    /// when high-volume pricing did not apply; null for transactions
    /// that predate HIP-1313.
    /// </summary>
    [JsonPropertyName("high_volume_pricing_multiplier")]
    [JsonConverter(typeof(NullableLongMirrorConverter))]
    public long? HighVolumePricingMultiplier { get; set; }
    /// <summary>
    /// The maximum fee the payer account was willing
    /// to spend on this transaction.
    /// </summary>
    [JsonPropertyName("max_fee")]
    [JsonConverter(typeof(FeeLimitFromStringConverter))]
    public long FeeLimit { get; set; }
    /// <summary>
    /// The limits the payer placed on the custom fees this
    /// transaction could be charged (HIP-991), one per fee
    /// denomination. Empty when the payer set none.
    /// </summary>
    [JsonPropertyName("max_custom_fees")]
    public CustomFeeLimitData[]? MaxCustomFees { get; set; }
    /// <summary>
    /// The memo that was attached to this transaction
    /// </summary>
    [JsonPropertyName("memo_base64")]
    [JsonConverter(typeof(Base64StringToBytesConverter))]
    public ReadOnlyMemory<byte> Memo { get; set; }
    /// <summary>
    /// The identified type of the transaction.
    /// </summary>
    [JsonPropertyName("name")]
    public string? TransactionType { get; set; }
    /// <summary>
    /// The individual NFT (asset) transfers carried out by this
    /// transaction. Null when the transaction moved no NFTs.
    /// </summary>
    [JsonPropertyName("nft_transfers")]
    public AssetTransferData[]? AssetTransfers { get; set; }
    /// <summary>
    /// The Hedera Gossip Node that submitted this transaction
    /// </summary>
    [JsonPropertyName("node")]
    public EntityId? GossipNode { get; set; }
    /// <summary>
    /// The nonce of the transaction (if it is a child tx)
    /// </summary>
    [JsonPropertyName("nonce")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Nonce { get; set; }
    /// <summary>
    /// The consensus timestamp of this transaction's
    /// parent transaction (if this is a child transaction)
    /// </summary>
    [JsonPropertyName("parent_consensus_timestamp")]
    public ConsensusTimeStamp? ParentConsensus { get; set; }
    /// <summary>
    /// The Status Code of this transaction.
    /// </summary>
    /// <remarks>
    /// A status the network reports but this build of the SDK does not know
    /// reads as <see cref="ResponseCode.Unrecognized"/> rather than failing
    /// the surrounding query.
    /// </remarks>
    [JsonPropertyName("result")]
    public ResponseCode Status { get; set; }
    /// <summary>
    /// Flag indicating that this was a scheduled transaction.
    /// </summary>
    [JsonPropertyName("scheduled")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool IsScheduled { get; set; }
    /// <summary>
    /// Enumerates any staking rewards attached
    /// to this transaction.
    /// </summary>
    [JsonPropertyName("staking_reward_transfers")]
    public StakingRewardTransferData[]? StakingRewards { get; set; }
    /// <summary>
    /// Enumerates the tokens that were transferred
    /// as a part of this transaction.
    /// </summary>
    [JsonPropertyName("token_transfers")]
    public TokenTransferData[]? TokenTransfers { get; set; }
    /// <summary>
    /// The transaction’s computed transaction hash.
    /// </summary>
    [JsonPropertyName("transaction_hash")]
    [JsonConverter(typeof(Base64StringToBytesConverter))]
    public ReadOnlyMemory<byte> Hash { get; set; }
    /// <summary>
    /// The list of cryptocurrency transfers associated with this transaction.
    /// </summary>
    [JsonPropertyName("transfers")]
    public CryptoTransferData[]? CryptoTransfers { get; set; }
    /// <summary>
    /// The valid time to live in seconds
    /// for the transaction.
    /// </summary>
    [JsonPropertyName("valid_duration_seconds")]
    [JsonConverter(typeof(ValidDurationInSecondsConverter))]
    public TimeSpan ValidDuration { get; set; }
    /// <summary>
    /// The transaction's valid starting timestamp.
    /// </summary>
    [JsonPropertyName("valid_start_timestamp")]
    public ConsensusTimeStamp ValidStarting { get; set; }
}
