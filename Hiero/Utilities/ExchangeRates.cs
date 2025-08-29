using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Object containing the current and next exchange rate
/// returned from the network.
/// </summary>
public sealed class ExchangeRates
{
    /// <summary>
    /// Internal constructor, used by the library to create an
    /// initialized exchange rates object.
    /// </summary>
    /// <param name="current">Current Exchange Rate</param>
    /// <param name="next">Next Exchange Rate</param>
    internal ExchangeRates(ExchangeRate? current, ExchangeRate? next)
    {
        Current = current;
        Next = next;
    }
    /// <summary>
    /// Current Exchange Rate
    /// </summary>
    public ExchangeRate? Current { get; }
    /// <summary>
    /// Exchange rate that is in effect after 
    /// the current exchange rate expires.
    /// </summary>
    public ExchangeRate? Next { get; }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ExchangeRatesExtensions
{
    /// <summary>
    /// Retrieves the current USD to hBar exchange rate information from the
    /// network.
    /// </summary>
    /// <remarks>
    /// NOTE: this method incours a charge to retrieve the file from the network.
    /// </remarks>
    /// <param name="client">ConsensusClient Object</param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// An Exchange Rates object providing the current and next 
    /// exchange rates.
    /// </returns>
    public static async Task<ExchangeRates> GetExchangeRatesAsync(this ConsensusClient client, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        // Well known address of the exchange rate file is 0.0.112
        var file = await client.GetFileContentAsync(new EntityId(0, 0, 112), cancellationToken, configure).ConfigureAwait(false);
        var set = Proto.ExchangeRateSet.Parser.ParseFrom(file.ToArray());
        return new ExchangeRates(set.CurrentRate?.ToExchangeRate(), set.NextRate?.ToExchangeRate());
    }
}