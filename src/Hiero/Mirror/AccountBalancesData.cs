// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;

/// <summary>
/// Structure identifying the hBar balance for
/// an associated account and the balances of a
/// limited number of tokens held by the account.
/// </summary>
/// <remarks>
/// NOTE this structure may not provide the entire
/// listing of tokens held by the associated account.
/// The mirror node caps the token list (HIP-367): at
/// most 1000 entries for a single account lookup and
/// at most 50 per account in an account listing.
/// </remarks>
public class AccountBalancesData
{
    /// <summary>
    /// Timestamp corresponding to this balance snapshot.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public ConsensusTimeStamp TimeStamp { get; set; }
    /// <summary>
    /// Crypto balance in tinybars.
    /// </summary>
    [JsonPropertyName("balance")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Balance { get; set; }
    /// <summary>
    /// Listing of token balance values for the associated
    /// account, capped by the mirror node (see remarks).
    /// </summary>
    [JsonPropertyName("tokens")]
    public TokenBalanceData[] Tokens { get; set; } = default!;
}
