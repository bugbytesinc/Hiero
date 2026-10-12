// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;
/// <summary>
/// A limit the payer placed on the custom fees a transaction could be
/// charged (HIP-991): the most the payer agreed to pay in one
/// denomination.
/// </summary>
public class CustomFeeLimitData
{
    /// <summary>
    /// The payer account the limit applies to.
    /// </summary>
    [JsonPropertyName("account_id")]
    public EntityId? Account { get; set; }
    /// <summary>
    /// The maximum amount, in the smallest unit of the denomination
    /// (tinybars for hbar).
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Amount { get; set; }
    /// <summary>
    /// The token the limit is denominated in, or null for hbar.
    /// </summary>
    [JsonPropertyName("denominating_token_id")]
    public EntityId? Token { get; set; }
}
