using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;

/// <summary>
/// Structure identifying the hBar balance for
/// an associated account and the balances of the
/// first 100 tokens held by the account.
/// </summary>
/// <remarks>
/// NOTE this structure may not provide the entire
/// listing of tokens held by the associated account.
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
    /// Listing of the first 100 token balance values
    /// for the associated account.
    /// </summary>
    [JsonPropertyName("tokens")]
    public TokenBalanceData[] Tokens { get; set; } = default!;
}
